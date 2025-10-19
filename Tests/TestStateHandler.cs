using System.Globalization;
using TelegramBotAPIExtensions.Core;
using TelegramBotAPIExtensions.Core.Attributes;
using TelegramBotAPIExtensions.Core.FSM;

namespace Tests;

public class TestStateHandler : StateHandler
{
    [StateCallback("wait_mes1")]
    public async Task TestHandleAsync(InteractionContext ctx)
    {
        Console.WriteLine($"User {ctx.From.Id} send {ctx.Message.Text}");
        _fsmService.ClearState(ctx.From.Id);    // Очистка состояния

        UserState? state = _fsmService.GetState(ctx.From.Id);   // Для проверки получим текущее состояние
        Console.WriteLine($"State is null after clear = {state == null}");
    }
}