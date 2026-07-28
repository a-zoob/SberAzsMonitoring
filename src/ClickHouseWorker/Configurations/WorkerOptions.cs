namespace SberAzsMonitoring.ClickHouseWorker.Configurations;

public class WorkerOptions
{
    public const string SectionName = "WorkerSettings";

    public string ClickHouseConnectionString { get; set; } = string.Empty;
    public string KafkaBootstrapServers { get; set; } = string.Empty;
    public string KafkaConsumerGroupId { get; set; } = "sberazs-clickhouse-shared-writer";

    // Список топиков через запятую, которые воркер будет вычитывать (например: "fuel-snapshots-pskov,fuel-snapshots-novgorod")
    public string KafkaTopicsToListen { get; set; } = string.Empty;
}
