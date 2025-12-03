namespace MiddleWare.DataStoreConnector.Broker
{
    /// <summary>
    /// 구독 채널키
    /// </summary>
    public enum PublisherKey
    {
        PublishChannelGameVersion,          //  게임 데이터 버전 업데이트
        PublishChannelClientVersion,        //  클라이언트 버전 업데이트
        PublishChannelWebSocketSendMessage, //  클라이언트에 메세지 전달
        PublishChannelWebSocketCloseMessage,//  클라이언트 연결 종료
        PublishNoticeUpdate,                //  공지사항 변경
        PublishChannelMaintenanceModeMessage,// 멘테넌스 모드
    }

    /// <summary>
    /// 구독 채널키
    ///     - redis의 경우 InstanceName가 앞에 붙여져서 사용해야함. (Ex: Middleware_PublishChannelGameVersion)
    /// </summary>
    public static class PublishKeyDefinitions
    {
        //Api 사용채널
        public static readonly PublisherKey[] ApiServerKeys = new[]
        {
            PublisherKey.PublishChannelGameVersion,
            PublisherKey.PublishChannelClientVersion,
            PublisherKey.PublishChannelWebSocketSendMessage,
            PublisherKey.PublishChannelWebSocketCloseMessage,
            PublisherKey.PublishNoticeUpdate,
            PublisherKey.PublishChannelMaintenanceModeMessage,
        };

        //Admin 사용채널
        public static readonly PublisherKey[] AdminServerKeys = new[]
        {
            PublisherKey.PublishChannelGameVersion,
            PublisherKey.PublishChannelClientVersion,
        };
    }

}
