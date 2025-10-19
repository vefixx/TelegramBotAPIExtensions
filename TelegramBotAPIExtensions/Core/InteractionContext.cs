using Telegram.BotAPI;
using Telegram.BotAPI.AvailableTypes;

namespace TelegramBotAPIExtensions.Core;

public class InteractionContext
{
    public TelegramBotClient Bot { get; }
    public Message Message { get; }
    public long ChatId => Message.Chat.Id;
    public User From => Message.From;

    public InteractionContext(TelegramBotClient client, Message message)
    {
        Bot = client;
        Message = message;
    }

}