using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MiddleWare.DataStoreConnector.Amazon
{
    /// <summary>
    /// SQS 관리자
    /// </summary>
    public class AwsSQS
    {
        /// <summary>
        /// 로거
        /// </summary>
        private readonly ILogger<AwsSQS>? logger;
        /// <summary>
        /// AWS SQS 관리자 
        /// </summary>
        public readonly AmazonSQSClient? sqsClient;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="configuration"></param>
        public AwsSQS(ILogger<AwsSQS> _logger, IConfiguration configuration)
        {
            logger = _logger;

            var accessKey = Environment.GetEnvironmentVariable("watchLogAccessKey");
            var secretKey = Environment.GetEnvironmentVariable("watchLogSecretKey");
            var _region = configuration["BrokerType:SqsConfigure:Region"];
            
            RegionEndpoint? region = AwsHelper.GetRegionEndpoint(_region);

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                logger?.LogWarning("AWS CloudWatch 로그 설정 실패: 키 또는 로그 그룹이 누락되었습니다.");
                return;
            }

            if (accessKey == null || secretKey == null || region == null)
            {
                logger?.LogWarning("{Service} Not Initialized", nameof(AwsSQS));
            }
            else
            {
                sqsClient = new AmazonSQSClient(accessKey, secretKey, region);
                logger?.LogDebug("{Service} Is Initialized", nameof(AwsSQS));
            }
        }


        /// <summary>
        /// 신규 큐 이름 생성
        /// </summary>
        /// <returns></returns>
        public string CreateRandomQueueName(string subProject)
        {
            string? projectName = Environment.GetEnvironmentVariable("PROJECT_NAME");
            string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "NoEnv";
            string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);

            return $"{environment}_{projectName}_{subProject}_{uniqueId}";
        }

        /// <summary>
        /// SQS 큐 생성
        ///     -SNS 수신정책을 설정
        /// </summary>
        /// <param name="queueName">생성할 큐 이름</param>
        /// <returns>생성된 큐의 URL, 실패 시 null</returns>
        /// <returns></returns>
        public async Task<string?> CreateSqsQueueAsync(string queueName, string queueArn, string topicArn)
        {
            try
            {
                int messageRetentionSeconds = 600;

                // SNS에서 이 SQS 큐로 메시지를 보낼 수 있도록 정책(Policy) 정의
                //    - 특정 SNS 주제(topicArn)에서만 메시지를 받을 수 있도록 제한
                string policy = $@"
                {{
                  ""Version"": ""2012-10-17"",
                  ""Statement"": [
                    {{
                      ""Sid"": ""AllowSNSToSendMessage"",
                      ""Effect"": ""Allow"",
                      ""Principal"": {{
                        ""Service"": ""sns.amazonaws.com""
                      }},
                      ""Action"": ""sqs:SendMessage"",
                      ""Resource"": ""{queueArn}"",
                      ""Condition"": {{
                        ""ArnEquals"": {{
                          ""aws:SourceArn"": ""{topicArn}""
                        }}
                      }}
                    }}
                  ]
                }}";

                // 큐 생성 요청에 정책 포함
                var request = new CreateQueueRequest
                {
                    QueueName = queueName,
                    Attributes = new Dictionary<string, string>
                    {
                        { "MessageRetentionPeriod", messageRetentionSeconds.ToString() }, // 메시지 보존 기간 600(10분)
                        { "VisibilityTimeout", "30" }, //기본 표시 제한 시간(30초)
                        { "ReceiveMessageWaitTimeSeconds", "10" }, // 메시지 수신 대기(10분)
                        { "DelaySeconds", "0" }, // 전송 지연
                        { "Policy", policy }
                    }
                };

                //신규 큐생성
                var response = await sqsClient!.CreateQueueAsync(request);
                return response.QueueUrl;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "큐 생성 실패: {QueueName}", queueName);
                return null;
            }
        }


        /// <summary>
        /// 큐 삭제
        /// </summary>
        /// <param name="queueUrl"></param>
        /// <returns></returns>
        public async Task<bool> DeleteSqsQueueAsync(string queueUrl)
        {
            try
            {
                await sqsClient!.DeleteQueueAsync(new DeleteQueueRequest
                {
                    QueueUrl = queueUrl
                });

                logger?.LogInformation("SQS Queue Deleted: {QueueUrl}", queueUrl);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to delete SQS queue: {QueueUrl}", queueUrl);
                return false;
            }
        }

    }
}
