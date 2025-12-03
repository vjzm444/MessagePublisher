using Microsoft.Extensions.Logging;
using MiddleWare.DataStoreConnector.Broker;

namespace AdminTool.BackgroundTasks
{
    /// <summary>
    /// 구독된 채널에서 받은 메시지를 처리.
    /// </summary>
    public class BrokerMessageDispatcher
    {
        private readonly ILogger<BrokerMessageDispatcher> _logger;

        /// <summary>
        /// 수행 할 액션
        /// </summary>
        private readonly Dictionary<PublisherKey, Func<string, Task>> _handlers;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="serviceVersionManager"></param>
        /// <param name="gameDataManager"></param>
        /// <param name="gameDataFetchService"></param>
        public BrokerMessageDispatcher(
            ILogger<BrokerMessageDispatcher> logger
        )
        {
            _logger = logger;

            //수행할 Action 정의
            _handlers = BuildHandlers();
        }

        /// <summary>
        /// 등록할 채널과 수행 할 액션
        /// </summary>
        /// <returns></returns>
        private Dictionary<PublisherKey, Func<string, Task>> BuildHandlers()
        {
            return new Dictionary<PublisherKey, Func<string, Task>>
            {
                { PublisherKey.PublishChannelGameVersion, _ => HandleGameVersion() },
                //{ PublisherKey.PublishChannelClientVersion, _ => serviceVersionManager.InitializeAsync() },
            };
        }

        /// <summary>
        /// 메시지 수신처리
        ///     - sqs로 설정된 경우 구독된채널이 아니여도 메시지 자체는 받게되어있음.
        /// </summary>
        /// <param name="_channelName"></param>
        public async Task DispatchAsync(string _channelName, string messages = "")
        {
            //정의된 채널에서왔는지?
            if (!Enum.TryParse(_channelName, out PublisherKey channelKey))
            {
                _logger.LogWarning("알 수 없는 채널에서 메시지 수신. 제목: {ChannelName}, 내용: {Messages}", _channelName, messages);
                return;
            }

            //  웹소켓 연결 끊기
            //  ㄴ 종료시킬 유저 아이디
            //  ㄴ 전체 사용자
            //  웹소켓 메세지 보내기
            //  ㄴ 단일 사용자
            //  ㄴ 전체 사용자
            if (_handlers.TryGetValue(channelKey, out var handler))
            {
                await handler(messages).ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning("정의되지 않은 채널에서 메시지 수신. 제목: {ChannelKey}, 내용: {Messages}", channelKey, messages);
            }

        }


        /// <summary>
        /// 게임버젼 업데이트 처리.
        /// </summary>
        /// <returns></returns>
        private async Task HandleGameVersion()
        {
            /*
            //Redis를 읽어들여, Version Setting
            await serviceVersionManager.InitializeAsync().ConfigureAwait(false);
            //게임버젼 목록
            string[] gameVersions = serviceVersionManager!.GetServiceGameDataVersion().OrderBy(x => x).Select(x => x.ToString()).ToArray();

            //Csv File => Local Save
            await gameDataFetchService.InitializeGameDataAsync(gameVersions).ConfigureAwait(false);

            //Memory Setting
            await gameDataManager.InitializeCsvMemory(gameVersions, true).ConfigureAwait(false);
            */
        }


    }

}
