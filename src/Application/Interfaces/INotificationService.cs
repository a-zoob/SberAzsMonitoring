namespace SberAzsMonitoring.Application.Interfaces;

public interface INotificationService
{
    // Отправка пуш-уведомления на мобильное устройство
    Task SendPushNotificationAsync(string message, string title = "Мониторинг АЗС", CancellationToken cancellationToken = default);
}
