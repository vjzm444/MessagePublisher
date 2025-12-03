using System.Text.Json.Serialization;

namespace MiddleWare.DataStoreConnector.Broker
{
    /// <summary>
    /// 수신된 메시지의 Body에 정의된 부분
    /// </summary>
    public class SnsNotificationMessage
    {
        [JsonPropertyName("Type")]
        public string? Type { get; set; }

        [JsonPropertyName("MessageId")]
        public string? MessageId { get; set; }

        [JsonPropertyName("TopicArn")]
        public string? TopicArn { get; set; }

        /// <summary>
        /// 메시지 키
        /// </summary>
        [JsonPropertyName("Subject")]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// 메시지 내용
        /// </summary>
        [JsonPropertyName("Message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("Timestamp")]
        public string? Timestamp { get; set; }

        [JsonPropertyName("SignatureVersion")]
        public string? SignatureVersion { get; set; }

        [JsonPropertyName("Signature")]
        public string? Signature { get; set; }

        [JsonPropertyName("SigningCertURL")]
        public string? SigningCertURL { get; set; }

        [JsonPropertyName("UnsubscribeURL")]
        public string? UnsubscribeURL { get; set; }
    }
}
