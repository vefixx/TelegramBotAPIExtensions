using System.Collections.Concurrent;
using System.Reflection;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.Extensions.Commands;
using Telegram.BotAPI.GettingUpdates;
using TelegramBotAPIExtensions.Core.Attributes;

namespace TelegramBotAPIExtensions.Core.Commands;


/// <summary>
/// Сервис для обработки слеш-команд
/// </summary>
public class SlashCommandsService
{
    /// <summary>
    /// Кэшированные объекты методов для команд
    /// </summary>
    private readonly ConcurrentDictionary<SlashCommandInfo, SlashCommandCallback> _callbacks = new();
    
    private readonly TelegramBotClient _client;
    
    private delegate Task SlashCommandCallback(InteractionContext ctx);
    
    private bool _callbacksIsLoaded = false;

    public SlashCommandsService(TelegramBotClient client)
    {
        _client = client;
    }
    
    /// <summary>
    /// Загружает все методы, которые имеют аттрибут <see cref="SlashCommandAttribute"/> и наследуют <see cref="InteractionHandler"/>
    /// </summary>
    private void LoadMethods()
    {
        // Получаем все классы, которые наследуют InteractionHandler
        Type targetType = typeof(InteractionHandler);
        var classesList = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => t.IsSubclassOf(targetType) && !t.IsAbstract && !t.IsInterface)
            .ToList();
        
        foreach (var classType in classesList)
        {
            var instance = Activator.CreateInstance(classType);
            try
            {
                var methods =
                    classType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var method in methods)
                {
                    var attribute = method.GetCustomAttribute<SlashCommandAttribute>();
                    if (attribute == null) continue;
                    
                    try
                    {
                        SlashCommandCallback delegateInstance = method.CreateDelegate<SlashCommandCallback>(instance);
                        _callbacks.TryAdd(new SlashCommandInfo(attribute.Command, attribute.Description), delegateInstance);
                    }
                    catch (Exception e)
                    {
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка обработки класса classType={classType}");
                continue;
            }
        }
        _callbacksIsLoaded = true;
    }

    public async Task<bool> RegisterCommandsAsync()
    {
        if (!_callbacksIsLoaded)
            LoadMethods();

        await _client.DeleteMyCommandsAsync();
        return await _client.SetMyCommandsAsync(_callbacks.Select(kv => new BotCommand(kv.Key.Command, kv.Key.Description)));
    }

    private SlashCommandCallback? GetCommandCallbackByName(string name)
    {
        SlashCommandInfo? slashCommandInfo = _callbacks.Keys.FirstOrDefault(i => i.Command == name);
        if (slashCommandInfo != null)
        {
            _callbacks.TryGetValue(slashCommandInfo, out var callback);
            return callback;
        }

        return null;
    }

    /// <summary>
    /// Попытка вызвать callback для сообщения (если оно имеет слеш-команду)
    /// </summary>
    /// <param name="update"></param>
    /// <returns></returns>
    public async Task<bool> TryExecuteCallbackAsync(Update update)
    {
        string commandExecuted;
        try
        {
            (commandExecuted, string? args, string? username) =
                BotCommandParser.Parse(update.Message);
        }
        catch (Exception e)
        {
            return false;
        }

        SlashCommandCallback? callback = GetCommandCallbackByName(commandExecuted);
        
        if (callback != null)
        {
            try
            {
                InteractionContext ctx = new InteractionContext(_client, update.Message);
                await callback(ctx);
                
                return true;
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        return false;
    }
}