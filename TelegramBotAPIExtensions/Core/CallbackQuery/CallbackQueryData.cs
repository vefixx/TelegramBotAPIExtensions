namespace TelegramBotAPIExtensions.Core.CallbackQuery;

public class CallbackQueryData
{
    public string? Data { get; }
    public Dictionary<string, string> Parameters { get; }
    public Telegram.BotAPI.AvailableTypes.CallbackQuery CallbackQuery { get; }

    public CallbackQueryData(string? data, Dictionary<string, string> parameters,
        Telegram.BotAPI.AvailableTypes.CallbackQuery callbackQuery)
    {
        Data = data;
        Parameters = parameters;
        CallbackQuery = callbackQuery;
    }
}