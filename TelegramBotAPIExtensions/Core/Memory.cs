using TelegramBotAPIExtensions.Core.Exceptions;
using TelegramBotAPIExtensions.Core.FSM;

namespace TelegramBotAPIExtensions.Core;

public class Memory
{
    private static FsmService? _fsmService = null;
    
    /// <summary>
    /// Объект <see cref="FsmService"/>. Необходим для сервиса, которые создает экземпляр контекста.
    /// Например, если была вызвана команда /start, при этом есть ее обработчик, наследуемый от <see cref="SlashCommand"/>,
    /// то будет создан новый экземпляр <see cref="SlashCommand"/> с переданным <see cref="FsmService"/>
    /// </summary>
    public static FsmService FsmService {
        get
        {
            if (_fsmService == null)
                throw new FsmIsNullException("FsmService не был установлен для его получения");
            return _fsmService;
        }
        set => _fsmService = value;
    }
}