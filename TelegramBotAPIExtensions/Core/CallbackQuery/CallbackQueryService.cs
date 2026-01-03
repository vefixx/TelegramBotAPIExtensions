using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.GettingUpdates;
using TelegramBotAPIExtensions.Core.Attributes;

namespace TelegramBotAPIExtensions.Core.CallbackQuery;

public class CallbackQueryService
{
    private delegate Task CallbackQueryHandler(InteractionContext ctx, CallbackQueryData callbackQueryData);
    // string - паттерн регулярного выражения для каллбека
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
                    if (attribute == null) 
                        continue;
                    
                    try
                    {
                        // Преобразуем шаблон типа {paramName} в регулярное выражение
                        string regexPattern = Regex.Replace(attribute.PatternData, @"\{(\w+)\}", @"(?<$1>[^:]+)");
                        CallbackQueryHandler delegateInstance = method.CreateDelegate<CallbackQueryHandler>(instance);
                        _callbacks.TryAdd(regexPattern, delegateInstance);
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
        
        // Проверяем каждый зарегистрированный шаблон
        foreach (var kv in _callbacks)
        {
            string pattern = kv.Key;
            CallbackQueryHandler handler = kv.Value;

            var match = Regex.Match(callbackData, "^" + pattern + "$");
            if (match.Success)
            {
                try
                {
                    // Получаем каждый параметр из callbackData
                    // Например, мы можем указать в callbackData строку типа
                    // "cat:{catName}:{id}", тогда сможем извлечь параметр catName и id
                    var parameters = new Dictionary<string, string>();
                    foreach (Group group in match.Groups)
                    {
                        if (group.Success && !string.IsNullOrEmpty(group.Name) && group.Name != "0")
                        {
                            parameters[group.Name] = group.Value;
                        }
                    }
                        
                    InteractionContext ctx = new InteractionContext(_client, update.Message);
                    CallbackQueryData callbackQueryData = new CallbackQueryData(callbackData, parameters, callbackQuery);
                    await handler(ctx, callbackQueryData);

                    return true;
                }
                catch (Exception e)
                {
                    continue;
                }
            }
        }
        
        //
        // if (_callbacks.TryGetValue(callbackData, out var callback))
        // {
        //     try
        //     {
        //         if (await _client.AnswerCallbackQueryAsync(callbackQuery.Id, showAlert: false))
        //         {
        //             InteractionContext ctx = new InteractionContext(_client, update.Message);
        //             CallbackQueryData callbackQueryData = new CallbackQueryData(callbackData);
        //             await callback(ctx, callbackQueryData);
        //
        //             return true;
        //         }
        //     }
        //     catch (Exception e)
        //     {
        //         // ignored
        //     }
        // }

        return false;
    }
}