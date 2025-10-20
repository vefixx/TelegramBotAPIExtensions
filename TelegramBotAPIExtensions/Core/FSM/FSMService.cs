using System.Collections.Concurrent;
using System.Reflection;
using Telegram.BotAPI;
using Telegram.BotAPI.GettingUpdates;
using TelegramBotAPIExtensions.Core.Attributes;

namespace TelegramBotAPIExtensions.Core.FSM;

/// <summary>
/// FSMService. Необходимо создавать один объект на всю сборку. 
/// </summary>
public class FsmService
{
    /// <summary>
    /// Текущие активные состояния
    /// </summary>
    private readonly ConcurrentDictionary<long, UserState> _states = new();

    /// <summary>
    /// Кэшированные объекты методов для состояний
    /// </summary>
    private readonly ConcurrentDictionary<string, StateCallback> _callbacks = new();

    private readonly TimeSpan _stateTimeout;
    private readonly TelegramBotClient _client;

    private bool _callbacksIsLoaded = false;

    private delegate Task StateCallback(InteractionContext ctx);

    /// <summary>
    /// FSM для управления состояниями пользователей. При создании экземпляра он передается в <see cref="Memory"/>.
    /// </summary>
    /// <param name="client">Клиент бота</param>
    /// <param name="stateTimeout">Время жизни бездействия состояния. По умолчанию устанавливается на 30 минут</param>
    public FsmService(TelegramBotClient client, TimeSpan? stateTimeout = null)
    {
        _client = client;

        if (stateTimeout != null)
            _stateTimeout = (TimeSpan)stateTimeout;
        else
            _stateTimeout = TimeSpan.FromMinutes(30);

        Memory.FsmService = this;
    }

    /// <summary>
    /// Очищает состояния, которые бездействуют более <c>stateTimeout</c>
    /// </summary>
    private void CleanupStates()
    {
        long[] expiredStates = _states
            .Where(kv => DateTime.Now - kv.Value.LastActivity > _stateTimeout)
            .Select(kv => kv.Key).ToArray();

        foreach (var userId in expiredStates)
        {
            ClearState(userId);
        }
    }

    /// <summary>
    /// Очистка состояния пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns></returns>
    public bool ClearState(long userId)
    {
        return _states.TryRemove(userId, out _);
    }

    /// <summary>
    /// Установка состояния
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="state">Новое состояние</param>
    public void SetState(long userId, string state)
    {
        CleanupStates();

        UserState userState = _states.GetOrAdd(userId, new UserState(state));
        userState.LastActivity = DateTime.Now;
        userState.State = state; // if (userState.State != state) {} ?
    }

    /// <summary>
    /// Возвращает объект <see cref="UserState"/> текущего состяния игрока
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <returns><see cref="UserState"/> - если состояние было найдено. Если состояние не было найдено, то возвращает <c>null</c></returns>
    public UserState? GetState(long userId) => _states.GetValueOrDefault(userId);

    /// <summary>
    /// Устанавливает новые данные в состояние пользователя. Данные не зависят от текущего State у объекта - то есть при его изменении данные остаются
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="key">Ключ, в котором будет храниться переданный объект</param>
    /// <param name="value">Значение</param>
    public void SetData(long userId, string key, object value)
    {
        if (_states.TryGetValue(userId, out var state))
        {
            state.Data[key] = value;
        }
    }

    /// <summary>
    /// Получает данные из объекта состояния пользователя
    /// </summary>
    /// <param name="userId">ID пользователя</param>
    /// <param name="key">Ключ, в котором хранится объект</param>
    /// <typeparam name="T">Тип данных, который должен вернуть метод</typeparam>
    /// <returns></returns>
    public T? GetData<T>(long userId, string key)
    {
        if (_states.TryGetValue(userId, out var state))
        {
            if (state.Data.TryGetValue(key, out var value))
            {
                return (T)value;
            }
        }

        return default;
    }
    
    /// <summary>
    /// Загружает все методы, которые имеют аттрибут <see cref="StateCallback"/> и наследуют <see cref="InteractionHandler"/>
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
                    var attribute = method.GetCustomAttribute<StateCallbackAttribute>();
                    if (attribute == null) continue;
                    
                    try
                    {
                        StateCallback delegateInstance = method.CreateDelegate<StateCallback>(instance);
                        _callbacks.TryAdd(attribute.State, delegateInstance);
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
    
    /// <summary>
    /// Попытка вызвать callback для указанного <paramref name="state"/>
    /// </summary>
    /// <param name="state"></param>
    /// <param name="update"></param>
    /// <returns></returns>
    public async Task<bool> TryExecuteCallbackAsync(string state, Update update)
    {
        if (!_callbacksIsLoaded)
            LoadMethods();

        
        if (_callbacks.TryGetValue(state, out var callback))
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