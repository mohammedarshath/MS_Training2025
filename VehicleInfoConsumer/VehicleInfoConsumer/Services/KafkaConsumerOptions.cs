namespace VehicleInfoConsumer.Services
{
    public sealed class KafkaConsumerOptions
    {
        private IConfiguration _configuration;
        public string BootstrapServers;
        public string Topic;
        public string GroupId;
        public string AutoOffsetReset;
        public bool EnableAutoCommit;
        public int SessionTimeoutMs;
        public int MaxPollIntervalMs;
        public int FetchMinBytes;
        public int FetchWaitMaxMs;
        public KafkaConsumerOptions()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            _configuration = builder.Build();
            // Override defaults with configuration values if available
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? BootstrapServers;
            Topic = _configuration["Kafka:Topic"] ?? Topic;
            GroupId = _configuration["Kafka:GroupId"] ?? GroupId;
            AutoOffsetReset = _configuration["Kafka:AutoOffsetReset"] ?? AutoOffsetReset;
            EnableAutoCommit = bool.TryParse(_configuration["Kafka:EnableAutoCommit"], out var enableAutoCommit) ? enableAutoCommit : EnableAutoCommit;
            SessionTimeoutMs = int.TryParse(_configuration["Kafka:SessionTimeoutMs"], out var sessionTimeoutMs) ? sessionTimeoutMs : SessionTimeoutMs;
            MaxPollIntervalMs = int.TryParse(_configuration["Kafka:MaxPollIntervalMs"], out var maxPollIntervalMs) ? maxPollIntervalMs : MaxPollIntervalMs;
            FetchMinBytes = int.TryParse(_configuration["Kafka:FetchMinBytes"], out var fetchMinBytes) ? fetchMinBytes : FetchMinBytes;
            FetchWaitMaxMs = int.TryParse(_configuration["Kafka:FetchWaitMaxMs"], out var fetchWaitMaxMs) ? fetchWaitMaxMs : FetchWaitMaxMs;
        }

        
    }
}
