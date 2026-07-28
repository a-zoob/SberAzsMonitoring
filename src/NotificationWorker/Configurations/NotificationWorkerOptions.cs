namespace SberAzsMonitoring.NotificationWorker.Configurations;

public sealed class NotificationWorkerOptions
{
    public string KafkaBootstrapServers { get; set; } = string.Empty;
    public string KafkaConsumerGroupId { get; set; } = string.Empty;
    public string KafkaTopicsToListen { get; set; } = string.Empty;

    // Адрес сервера ntfy (теперь будет принимать http://ntfy-server)
    public string NtfyBaseUrl { get; set; } = "https://ntfy.sh";

    // Добавляем Мастер-токен для авторизации на собственном сервере
    public string NtfyAdminPassword { get; set; } = string.Empty;
}
