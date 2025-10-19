namespace TelegramBotAPIExtensions.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class MessageCallbackAttribute : Attribute
{
    public string Content { get; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="content">Контент, на которое будет реагировать обработчик</param>
    public MessageCallbackAttribute(string content)
    {
        Content = content;
    }
}