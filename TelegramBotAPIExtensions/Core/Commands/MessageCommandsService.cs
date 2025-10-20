using System.Collections.Concurrent;
using System.Reflection;
using Telegram.BotAPI;
using Telegram.BotAPI.Extensions.Commands;
using Telegram.BotAPI.GettingUpdates;
using TelegramBotAPIExtensions.Core.Attributes;

namespace TelegramBotAPIExtensions.Core.Commands;

public class MessageCommandsService
{
    /// <summary>
    /// Кэшированные объекты методов для команд
    /// </summary>
    private readonly ConcurrentDictionary<string, MessageCallback> _callbacks = new();
    
    private readonly TelegramBotClient _client;
    
    private delegate Task MessageCallback(InteractionContext ctx);
    
    private bool _callbacksIsLoaded = false;
    
    public MessageCommandsService(TelegramBotClient client)
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
                    var attribute = method.GetCustomAttribute<MessageCallbackAttribute>();
                    if (attribute == null) continue;
                    
                    try
                    {
                        MessageCallback delegateInstance = method.CreateDelegate<MessageCallback>(instance);
                        _callbacks.TryAdd(attribute.Content, delegateInstance);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                continue;
            }
        }
        _callbacksIsLoaded = true;
    }
    
    /// <summary>
    /// Попытка вызвать callback для сообщения
    /// </summary>
    /// <param name="update"></param>
    /// <returns></returns>
    public async Task<bool> TryExecuteCallbackAsync(Update update)
    {
        if (!_callbacksIsLoaded)
            LoadMethods();
        
        if (_callbacks.TryGetValue(update.Message.Text, out var callback))
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