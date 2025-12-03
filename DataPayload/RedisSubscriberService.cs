using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiddleWare.DataStoreConnector.Broker;
using StackExchange.Redis;

namespace AdminTool.BackgroundTasks
{
    /// <summary>
    /// 레디스 구독/수신 처리
    /// </summary>
    public class RedisSubscriberService : BackgroundService
    {
        /// <summary>
        /// 로거
        /// </summary>
        private readonly ILogger<RedisSubscriberService> _logger;
        /// <summary>
        /// redis 메시지 발행자
        /// </summary>
        private readonly ISubscriber _subscriber;
        /// <summary>
        /// 메시지 공통처리
        /// </summary>
        private readonly BrokerMessageDispatcher dispatcher;
        /// <summary>
        /// 구독 할 채널
        /// </summary>
        private readonly List<string> subscribeChannel;
        /// <summary>
        /// 레디스 인스턴스명
        /// </summary>
        private readonly string redisInstanceName;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="_dispatcher"></param>
        public RedisSubscriberService(ISubscriber subscriber, ILogger<RedisSubscriberService> logger, IConfiguration configuration, BrokerMessageDispatcher _dispatcher)
        {
            _subscriber = subscriber;
            _logger = logger;
            dispatcher = _dispatcher;
            redisInstanceName = configuration.GetValue<string>("RedisConnection:InstanceName")!;

            subscribeChannel = new List<string>();
            //구독 할 채널목록
            foreach (var key in PublishKeyDefinitions.AdminServerKeys)
            {
                subscribeChannel.Add($"{redisInstanceName}{key.ToString()}");
            }

        }

        /// <summary>
        /// 채널 등록, 구독 지속상태
        ///     - 계속 실행된다
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // 구독할 채널 지정
            foreach (string channelName in subscribeChannel)
            {
                //채널설정
                RedisChannel redisChannel = new RedisChannel(channelName, RedisChannel.PatternMode.Literal);

                await _subscriber.SubscribeAsync(redisChannel, async (channel, message) =>
                {
                    _logger.LogInformation("[{ChannelName}] 채널에서 수신한 메시지 :: {Message}", channelName, message);

                    //기본 인스턴스이름은 제거.
                    string channelKey = channelName.Substring(redisInstanceName.Length);

                    // 메시지가 수신되면, 구독된 채널에 따라 처리.
                    await dispatcher.DispatchAsync(channelKey).ConfigureAwait(false);


                }).ConfigureAwait(false);
            }

            // 구독을 계속 유지하기 위한 무한 대기 (중지될 때까지 계속 실행)
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }

        /// <summary>
        /// 모든 redis 구독을 해제한다.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RedisSubscriberService is stopping.");

            try
            {
                // Unsubscribe from all channels
                await _subscriber.UnsubscribeAllAsync().ConfigureAwait(false);
                _logger.LogInformation("Unsubscribed from all Redis channels.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while unsubscribing from Redis channels.");
            }

            _logger.LogInformation("RedisSubscriberService is stopped.");
        }
    }
}
