using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiddleWare.DataStoreConnector.Amazon;
using MiddleWare.DataStoreConnector.Broker;
using System.Text.Json;

namespace AdminTool.BackgroundTasks
{
    /// <summary>
    /// 큐 메시지 수신
    /// </summary>
    public class SQSSubscriberService : BackgroundService, IDisposable
    {
        
        /// <summary>
        /// 로거
        /// </summary>
        private readonly ILogger<SQSSubscriberService> logger;

        /// <summary>
        /// SNS Topic 관리
        /// </summary>
        private readonly AwsSNS snsClient;

        /// <summary>
        /// SQS 큐 관리
        /// </summary>
        private readonly AwsSQS sqsClient;
        /// <summary>
        /// 수신메시지 처리자
        /// </summary>
        private readonly BrokerMessageDispatcher messageDispatcher;

        /// <summary>
        /// 큐 Arn
        /// </summary>
        public readonly string queueArn;

        /// <summary>
        /// 이벤트 큐 URL(기존 큐)
        /// </summary>
        public readonly string eventQueueUrl;

        /// <summary>
        /// 서버 생성 큐 URL
        /// </summary>
        public string RuntimeQueueUrl { get; private set; } = string.Empty;
        
        /// <summary>
        /// 서버 생성 큐 구독 Arn
        /// </summary>
        public string RuntimeSubscriptionArn { get; private set; } = string.Empty;

        /// <summary>
        /// 큐 정상준비 플러그
        /// </summary>
        public bool IsSqsConfigured { get; private set; }

        /// <summary>
        /// disposed
        /// </summary>
        private bool disposedValue;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="_logger"></param>
        /// <param name="_awsSNS"></param>
        /// <param name="_awsSQS"></param>
        /// <param name="configuration"></param>
        /// <param name="_dispatcher"></param>
        public SQSSubscriberService(ILogger<SQSSubscriberService> _logger, AwsSNS _awsSNS, AwsSQS _awsSQS, IConfiguration configuration, BrokerMessageDispatcher _dispatcher)
        {
            logger = _logger;
            snsClient = _awsSNS;
            sqsClient = _awsSQS;
            messageDispatcher = _dispatcher;

            eventQueueUrl = configuration["BrokerType:SqsConfigure:EventQueueUrl"]!;
            queueArn = configuration["BrokerType:SqsConfigure:QueueArn"]!;
        }

        /// <summary>
        /// Sqs 생성 및 Sns구독 설정
        /// </summary>
        /// <returns></returns>
        public async override Task StartAsync(CancellationToken cancellationToken)
        {
            string runtimeEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? string.Empty;
            
            //개발모드면 실시간처리를 하지않음
            if (runtimeEnv.Equals("Development"))
                return;

            string subProjectName = "Admin";
            //1. 고유한 큐 이름 생성
            string newQueueName = sqsClient.CreateRandomQueueName(subProjectName);
            string _runtimeQueueArn = $"{queueArn}:{newQueueName}";

            //2. 큐 생성
            string? newQueueUrl = await sqsClient.CreateSqsQueueAsync(newQueueName, _runtimeQueueArn, snsClient.topicArn);
            if (string.IsNullOrEmpty(newQueueUrl))
            {
                logger?.LogWarning("큐 생성 실패. 이름: [{NewQueueName}], Arn: [{RuntimeQueueArn}], Topic [{SnsClient.topicArn}]", newQueueName, _runtimeQueueArn, snsClient.topicArn);
                return;
            }
            
            //3. SNS 토픽에 신규큐 구독등록
            string? _subscriptionArn = await snsClient.SubscribeSqsQueueToSnsAsync("sqs", _runtimeQueueArn);
            if (string.IsNullOrEmpty(_subscriptionArn))
            {
                logger?.LogWarning("SNS 구독 실패: 큐arn: [{RuntimeQueueArn}], Topic: [{SnsClient.topicArn}]", _runtimeQueueArn, snsClient.topicArn);
                return;
            }

            RuntimeQueueUrl = newQueueUrl;
            RuntimeSubscriptionArn = _subscriptionArn;

            //큐 준비완료 플러그 설정
            IsSqsConfigured = true;

            await base.StartAsync(cancellationToken); //ExecuteAsync 호출
        }

        /// <summary>
        /// 실행자
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var task1 = PollMessagesAsync(RuntimeQueueUrl, stoppingToken);
                var task2 = PollMessagesAsync(eventQueueUrl, stoppingToken);

                logger?.LogInformation("신규 큐[{RuntimeQueueUrl}] 및 이벤트 큐[{EventQueueUrl}]가 구독메시지 수신을 시작하였습니다.", RuntimeQueueUrl, eventQueueUrl);

                await Task.WhenAll(task1, task2);

            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "큐[{RuntimeQueueUrl}]가 준비되어 있지않아 폴링에 실패하였습니다. Error: {Error}", RuntimeQueueUrl, ex.Message);
            }
        }

        /// <summary>
        /// 메시지 폴링
        /// </summary>
        /// <param name="queueUrl"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task PollMessagesAsync(string queueUrl, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var response = await sqsClient.sqsClient!.ReceiveMessageAsync(new ReceiveMessageRequest
                    {
                        QueueUrl = queueUrl,
                        MaxNumberOfMessages = 10,
                        WaitTimeSeconds = 10
                    }, token);

                    foreach (var message in response.Messages)
                    {
                        // 메시지 파싱
                        SnsNotificationMessage notiInfo = JsonSerializer.Deserialize<SnsNotificationMessage>(message.Body)!;

                        logger.LogInformation("[{QueueUrl}] 메시지 수신 Title: {Subject}, Message: {Message}", queueUrl, notiInfo.Subject, notiInfo.Message);

                        // 메시지가 수신되면, 구독된 채널에 따라 처리.(title, message)
                        await messageDispatcher.DispatchAsync(notiInfo.Subject).ConfigureAwait(false);

                        // SQS에서 메시지를 정상적으로 처리했다는 신호 전달
                        await sqsClient.sqsClient!.DeleteMessageAsync(queueUrl, message.ReceiptHandle, token);
                    }
                }
                catch (TaskCanceledException)
                {
                    // 무시
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[{QueueUrl}] 메시지 처리 중 예외 발생. Error : {Error}", queueUrl, ex.Message);
                }
            }
        }


        /// <summary>
        /// 서버가 종료되었을때
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            //큐가 정상실행일때만 실행
            if (IsSqsConfigured)
            {
                //큐 구독 해지
                bool isUnSubscribe = await snsClient.UnSubscribeAsync(RuntimeSubscriptionArn);

                //큐 삭제
                if (isUnSubscribe)
                {
                    await sqsClient.DeleteSqsQueueAsync(RuntimeQueueUrl);
                    logger.LogInformation("큐 서비스 [{RuntimeQueueUrl}] 구독해제 및 삭제가 정상적으로 처리되었습니다.", RuntimeQueueUrl);
                }
            }
        }


        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }
                disposedValue = true;
            }
        }


        /// <summary>
        /// Dispose
        /// </summary>
        public override void Dispose()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
