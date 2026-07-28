namespace SberAzsMonitoring.Application.Common.Configurations;

public class RegionOptions
{
    public const string SectionName = "RegionSettings";

    public string Name { get; set; } = "Псков";
    public string SberAzsEndpoint { get; set; } = string.Empty;
    public string NtfyTopicUrl { get; set; } = string.Empty;

    // Новые параметры конфигурации для Этапа 2
    public string KafkaBootstrapServers { get; set; } = "localhost:29092";
    public string KafkaTopicName { get; set; } = "sberazs-fuel-pskov";
    public int KafkaTopicRetentionDays { get; set; } = 2; // Значение по умолчанию — 2 суток
    public string KafkaConsumerGroupId { get; set; } = "sberazs-clickhouse-writer-group";
    public string ClickHouseConnectionString { get; set; } = string.Empty;
    public string NtfyBaseUrl { get; set; } = string.Empty;
}
