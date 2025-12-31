namespace TelegramBotAPIExtensions.Core.CallbackQuery;

public class CallbackQueryData
{
    public string? Data { get; }

    public CallbackQueryData(string? data)
    {
        Data = data;
    }
}