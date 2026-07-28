namespace SberAzsMonitoring.Application.Common.DTOs;

public record AnalysisResultDto(
    bool ShouldNotify,       // Флаг: критично ли изменение цен
    string AlertMessage,     // Сгенерированный ИИ текст для push-уведомления ntfy.sh
    string DetailedReason    // Логическое обоснование от ИИ для логов системы
);
