namespace TelegramBotAPIExtensions.Core.CallbackQuery;

public class CallbackQueryData
{
    public string? Data { get; }
    public Dictionary<string, string> Parameters { get; }

    public CallbackQueryData(string? data, Dictionary<string, string> parameters)
    {
        Data = data;
        Parameters = parameters;
    }
}