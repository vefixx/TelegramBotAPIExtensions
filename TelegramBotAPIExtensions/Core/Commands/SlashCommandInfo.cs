namespace TelegramBotAPIExtensions.Core.Commands;

public class SlashCommandInfo
{
    public string Command { get; }
    public string Description { get; }

    public SlashCommandInfo(string command, string description)
    {
        Command = command;
        Description = description;
    }
}