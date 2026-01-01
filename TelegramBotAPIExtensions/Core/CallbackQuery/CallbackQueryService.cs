using System.Collections.Concurrent;
using System.Reflection;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.GettingUpdates;
using TelegramBotAPIExtensions.Core.Attributes;

namespace TelegramBotAPIExtensions.Core.CallbackQuery;

public class CallbackQueryService
{
    private delegate Task CallbackQueryHandler(InteractionContext ctx, CallbackQueryData callbackQueryData);
    private readonly ConcurrentDictionary<string, CallbackQueryHandler> _callbacks = new();
    private bool _callbacksIsLoaded = false;
    
    private readonly TelegramBotClient _client;


    public CallbackQueryService(TelegramBotClient client)
    {
        _client = client;
    }
    
    /// <summary>
    /// Загружает все методы, которые имеют аттрибут <see cref="CallbackQueryAttribute"/> и наследуют <see cref="InteractionHandler"/>
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
                    var attribute = method.GetCustomAttribute<CallbackQueryAttribute>();
                    if (attribute == null) continue;
                    
                    try
                    {
                        CallbackQueryHandler delegateInstance = method.CreateDelegate<CallbackQueryHandler>(instance);
                        _callbacks.TryAdd(attribute.TargetData, delegateInstance);
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
    /// Попытка вызвать callback
    /// </summary>
    /// <param name="update"></param>
    /// <param name="callbackQuery"></param>
    /// <returns></returns>
    public async Task<bool> TryExecuteCallbackAsync(Update update, Telegram.BotAPI.AvailableTypes.CallbackQuery callbackQuery)
    {
        if (!_callbacksIsLoaded)
            LoadMethods();
        
        var callbackData = callbackQuery.Data;

        if (_callbacks.TryGetValue(callbackData, out var callback))
        {
            try
            {
                if (await _client.AnswerCallbackQueryAsync(callbackQuery.Id, showAlert: false))
                {
                    InteractionContext ctx = new InteractionContext(_client, update.Message);
                    CallbackQueryData callbackQueryData = new CallbackQueryData(callbackData);
                    await callback(ctx, callbackQueryData);

                    return true;
                }
            }
            catch (Exception e)
            {
                // ignored
            }
        }

        return false;
    }
}