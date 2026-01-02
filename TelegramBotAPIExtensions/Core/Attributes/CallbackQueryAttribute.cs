namespace TelegramBotAPIExtensions.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class CallbackQueryAttribute : Attribute
{
    public string PatternData { get; }

    public CallbackQueryAttribute(string patternData)
    {
        PatternData = patternData;
    }
}