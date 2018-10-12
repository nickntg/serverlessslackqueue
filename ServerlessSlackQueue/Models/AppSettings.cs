namespace ServerlessSlackQueue.Models
{
    public class AppSettings
    {
        public string SigningSecret { get; set; }
        public string SqsUrlPrefix { get; set; }
        public int NewQueueMessageRetentionPeriod { get; set; }
        public int NewQueueVisibilityTimeout { get; set; }
    }
}