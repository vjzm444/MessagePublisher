using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MiddleWare.DataStoreConnector.Broker;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MiddleWare.DataStoreConnector.Redis
{
    /// <summary>
    /// Redis 메시지 전송 관리
    /// </summary>
    public partial class RedisMessagePublisher : IBrokerMessagePublisher
    {
        /// <summary>
        /// 로거
        /// </summary>
        private ILogger<RedisMessagePublisher> logger;
        /// <summary>
        /// 인스턴스 이름
        /// </summary>
        private readonly string instanceName;
        /// <summary>
        /// 전송자
        /// </summary>
        private readonly ISubscriber subscriber;

        /// <summary>
        /// Dependency Injection(DI)
        /// 객체를 생성하지 말자.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="redisConnection"></param>
        public RedisMessagePublisher(ILogger<RedisMessagePublisher> logger, IConfiguration configuration, IConnectionMultiplexer redisConnection)
        {
            this.logger = logger;

            instanceName = configuration.GetValue<string>("RedisConnection:InstanceName") ?? string.Empty;

            //일단 로그만 남긴다.
            if (string.IsNullOrEmpty(instanceName))
                logger.LogWarning("RedisMessagePublisher DI Error. redisInstanceName: {InstanceName}", instanceName);

            // Redis에 메시지 발행
            subscriber = redisConnection.GetSubscriber();
        }

        /// <summary>
        /// 메시지 송신
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channelName"></param>
        /// <param name="payload"></param>
        public void Publisher<T>(PublisherKey channelName, T payload) where T : class
        {
            PublishAsyn(channelName.ToString(), JsonConvert.SerializeObject(payload));
        }

        /// <summary>
        /// 메시지 송신
        ///     - redis: RedisSubscriberService에서 메시지를 수신처리
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private void PublishAsyn(string channelName, string message = "")
        {
            string _channel = $"{instanceName}{channelName}";

            //채널설정(Middleware_채널이름)
            RedisChannel redisChannel = new RedisChannel(_channel, RedisChannel.PatternMode.Literal);

            // 전송(대기X)
            Task.Run(async () =>
                await subscriber.PublishAsync(redisChannel, message)
            );
            //subscriber.PublishAsync(redisChannel, message).FireAndForget(ex => logger.LogError(ex, "Redis 발송 실패"));
        }

    }
}
