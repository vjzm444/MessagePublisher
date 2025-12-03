using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Amazon;
using Amazon.SimpleNotificationService.Model;


namespace MiddleWare.DataStoreConnector.Amazon
{
    /// <summary>
    /// Aws Simple Notification Services
    /// </summary>
    public class AwsSNS
    {
        /// <summary>
        /// 로거
        /// </summary>
        private ILogger<AwsSNS>? logger;

        /// <summary>
        /// sns 전송관리자
        /// </summary>
        private AmazonSimpleNotificationServiceClient? snsClient;

        /// <summary>
        /// SNS 토픽
        /// </summary>
        public readonly string topicArn;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="configuration"></param>
        public AwsSNS(ILogger<AwsSNS> _logger, IConfiguration configuration)
        {
            logger = _logger;

            var accessKey = Environment.GetEnvironmentVariable("watchLogAccessKey");
            var secretKey = Environment.GetEnvironmentVariable("watchLogSecretKey");
            topicArn = configuration["BrokerType:Sns:TopincArn"]!;

            var _region = configuration["BrokerType:Sns:Region"]; // 서울 리전
            RegionEndpoint? region = AwsHelper.GetRegionEndpoint(_region);

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
            {
                logger?.LogWarning("AWS CloudWatch 로그 설정 실패: 키 또는 로그 그룹이 누락되었습니다.");
                return;
            }

            if (accessKey == null || secretKey == null || region == null)
            {
                logger?.LogWarning("{Service} Not Initialized", nameof(AwsSNS));
            }
            else
            {
                snsClient = new AmazonSimpleNotificationServiceClient(accessKey, secretKey, region);
                logger?.LogDebug("{Service} Is Initialized", nameof(AwsSNS));
            }
        }


        /// <summary>
        /// 메세지와 제목을 포함해서 Sns Publish
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="message"></param>
        public async Task<bool> PublishMessageAsync(string message, string subject)
        {
            if (string.IsNullOrEmpty(topicArn))
            {
                return false;
            }

            if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(subject))
            {
                logger?.LogWarning("Message Or Subject Is Null Or Empty");
                return false;
            }

            var response = await snsClient!.PublishAsync(topicArn, message, subject);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                logger?.LogWarning("PublishMessageAsync TopicArn: {TopicArn}, HttpStatusCode: {ResponseHttpStatusCode}", topicArn, response.HttpStatusCode);
                return false;
            }
        }

        /// <summary>
        /// 메세지를 포함해서 Sns Publish
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<bool> PublishMessageAsync(string message)
        {
            if (string.IsNullOrEmpty(topicArn))
            {
                return false;
            }

            if (string.IsNullOrEmpty(message))
            {
                logger?.LogWarning("Message Is Null Or Empty");
                return false;
            }

            var response = await snsClient!.PublishAsync(topicArn, message);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                logger?.LogWarning("PublishMessageAsync TopicArn: {TopicArn}, HttpStatusCode: {ResponseHttpStatusCode}", topicArn, response.HttpStatusCode);
                return false;
            }
        }

        /// <summary>
        /// 구독자 목록을 가져오기
        /// </summary>
        /// <returns></returns>
        public async Task<List<Subscription>?> ListsubscribeAsync()
        {
            var response = await snsClient!.ListSubscriptionsByTopicAsync(topicArn);

            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return response.Subscriptions;
            }
            else
            {
                logger?.LogWarning("{HandleName} TopicArn: {TopicArn}, HttpStatusCode: {ResponseHttpStatusCode}", nameof(ListsubscribeAsync), topicArn, response.HttpStatusCode);
                return null;
            }
        }

        /// <summary>
        /// 구독자 등록
        /// </summary>
        /// <param name="targetArn"></param>
        /// <returns></returns>
        public async Task<string?> SubscribeSqsQueueToSnsAsync(string protocol, string targetArn)
        {
            // 3. Subscribe SQS queue to SNS topic
            var subscribeResponse = await snsClient!.SubscribeAsync(new SubscribeRequest
            {
                TopicArn = topicArn,
                Protocol = protocol,
                Endpoint = targetArn
            });

            if (subscribeResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                logger?.LogInformation("Subscribed {Protocol} to SNS. SubscriptionArn: {SubscriptionArn}", protocol, subscribeResponse.SubscriptionArn);
                return subscribeResponse.SubscriptionArn;
            }
            else
            {
                logger?.LogWarning("Subscription failed. Status: {HttpStatusCode}", subscribeResponse.HttpStatusCode);
                return null;
            }
        }

        /// <summary>
        /// 구독 해제
        /// </summary>
        /// <param name="subscribeArn"></param>
        /// <returns></returns>
        public async Task<bool> UnSubscribeAsync(string subscribeArn)
        {
            var response = await snsClient!.UnsubscribeAsync(subscribeArn);
            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                logger?.LogWarning("{HandleName} TopicArn: {TopicArn}, SubscribeArn: {SubscribeArn}, HttpStatusCode: {ResponseHttpStatusCode}", nameof(UnSubscribeAsync), topicArn, subscribeArn, response.HttpStatusCode);
                return false;
            }
        }
    }
}
