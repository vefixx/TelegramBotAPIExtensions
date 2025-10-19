using Telegram.BotAPI;

namespace TelegramBotAPIExtensions.Core.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class SlashCommandAttribute : Attribute
{
    public string Command { get; }
    public string Description { get; }

    public SlashCommandAttribute(string command, string description)
    {
        if (string.IsNullOrEmpty(description))
        {
            throw new Exception("Описание команды не должно быть пустым");
        }
        
        Command = command;
        Description = description;
    }
}