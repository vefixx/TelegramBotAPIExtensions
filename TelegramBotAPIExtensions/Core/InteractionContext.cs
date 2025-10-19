using Telegram.BotAPI;
using Telegram.BotAPI.AvailableTypes;

namespace TelegramBotAPIExtensions.Core;

public class InteractionContext
{
    public TelegramBotClient Bot { get; }
    public Message Message { get; }
    public long ChatId => Message.Chat.Id;
    public User From => Message.From;

    public InteractionContext(TelegramBotClient bot, Message message)
    {
        Bot = bot;
        Message = message;
    }

}