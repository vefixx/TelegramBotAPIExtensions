namespace TelegramBotAPIExtensions.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class StateCallbackAttribute : Attribute
{
    public string State { get; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="state">Состояние, на которое будет реагировать обработчик</param>
    public StateCallbackAttribute(string state)
    {
        State = state;
    }
}