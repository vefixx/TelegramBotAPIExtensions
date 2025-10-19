namespace TelegramBotAPIExtensions.Core.FSM;

public class UserState
{
    public string State { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public DateTime LastActivity { get; set; } = DateTime.Now;

    public UserState(string state)
    {
        State = state;
    }
}