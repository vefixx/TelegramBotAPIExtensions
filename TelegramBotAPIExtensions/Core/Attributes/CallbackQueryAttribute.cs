namespace TelegramBotAPIExtensions.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CallbackQueryAttribute : Attribute
{
    public string TargetData { get; }

    public CallbackQueryAttribute(string targetData)
    {
        TargetData = targetData;
    }
}