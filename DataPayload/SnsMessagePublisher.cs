using Microsoft.Extensions.Logging;
using MiddleWare.DataStoreConnector.Broker;
using Newtonsoft.Json;

namespace MiddleWare.DataStoreConnector.Amazon
{
    /// <summary>
    /// SNS 메시지 전송관리
    /// </summary>
    public partial class SnsMessagePublisher : IBrokerMessagePublisher
    {
        /// <summary>
        /// 로거
        /// </summary>
        private ILogger<SnsMessagePublisher> logger;
        /// <summary>
        /// SNS 전송
        /// </summary>
        private readonly AwsSNS awsSNS;


        /// <summary>
        /// Dependency Injection(DI)
        /// 객체를 생성하지 말자.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        public SnsMessagePublisher(ILogger<SnsMessagePublisher> logger, AwsSNS _awsSNS)
        {
            this.logger = logger;
            awsSNS = _awsSNS;
        }

        /// <summary>
        /// 메시지 송신
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channelName"></param>
        /// <param name="payload"></param>
        public void Publisher<T>(PublisherKey channelName, T payload) where T : class
        {
            Publisher(channelName.ToString(), JsonConvert.SerializeObject(payload));
        }


        /// <summary>
        /// 메시지 송신
        ///     - sns: SQSSubscriberService 에서 메시지를 수신처리
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private void Publisher(string channelName, string message = "")
        {
            // 전송(대기X)
            Task.Run(async () =>
                await awsSNS.PublishMessageAsync(message, channelName)
            );

        }
    }
}
