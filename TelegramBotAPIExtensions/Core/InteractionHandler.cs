using Telegram.BotAPI;
using Telegram.BotAPI.AvailableTypes;
using TelegramBotAPIExtensions.Core.FSM;

namespace TelegramBotAPIExtensions.Core;


/// <summary>
/// Класс, который должен наследовать каждый обработчик состояний. По нему происходит процесс поиска метода обработчика текущего состояния
/// </summary>
public abstract class InteractionHandler
{
    protected FsmService _fsmService = Memory.FsmService;
}