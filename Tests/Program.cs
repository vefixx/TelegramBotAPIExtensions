using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.GettingUpdates;
using TelegramBotAPIExtensions.Core.Commands;
using TelegramBotAPIExtensions.Core.FSM;

namespace Tests;

class Program
{
    private TelegramBotClient _client;
    private long _testUserId = 0;
    
    static async Task Main(string[] args)
    {
        Console.WriteLine(Directory.GetCurrentDirectory());
        DotNetEnv.Env.Load();
        await new Program().RunClientAsync();
    }

    public async Task RunClientAsync()
    {
        _client = new TelegramBotClient(DotNetEnv.Env.GetString("TOKEN"));
        Console.WriteLine($"Start {_client.GetMe().Username}");
        
        // Обязательное создание экземпляра FSM. В конструкторе он будет автоматически установлен в Memory
        FsmService fsmService = new FsmService(_client);
        SlashCommandsService slashCommandsService = new SlashCommandsService(_client);
        await slashCommandsService.RegisterCommandsAsync();
        
        // Устанавливаем тестовое состояние
        fsmService.SetState(_testUserId, "wait_mes1");
        
        // Слушаем обновления
        var updates = await _client.GetUpdatesAsync();
        if (updates.Any())
            updates = await _client.GetUpdatesAsync(updates.Last().UpdateId + 1);
        
        while (true)
        {
            if (updates.Any())
            {
                foreach (var update in updates)
                {
                    // Проверяем, что у пользователя установлено состояние
                    if (!await slashCommandsService.TryExecuteCallbackAsync(update))
                    {
                        UserState? state = fsmService.GetState(_testUserId);
                        if (state != null)
                        {
                            // В нашем случае вызовется метод TestInteractionHandler.TestHandleAsync
                            bool stateIsExecuted = await fsmService.TryExecuteCallbackAsync(state.State, update);
                            Console.WriteLine($"Executed: {stateIsExecuted}");
                        }
                    }
                }
                updates = await _client.GetUpdatesAsync(updates.Last().UpdateId + 1);
            }
            else
            {
                updates = await _client.GetUpdatesAsync();
            }
        }
    }
}