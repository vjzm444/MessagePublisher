
using AdminTool.BackgroundTasks;
using Microsoft.Extensions.DependencyInjection;
using MiddleWare.DataStoreConnector.Amazon;
using MiddleWare.DataStoreConnector.Broker;
using MiddleWare.DataStoreConnector.Redis;
//var builder = WebApplication.CreateBuilder(args);
var runtimeEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var services = new ServiceCollection();


#region Configuration Setting

string _brokerType = Environment.GetEnvironmentVariable("BrokerType") ?? string.Empty;

switch (_brokerType)
{
    case "redis":
        services.AddHostedService<RedisSubscriberService>(); //구독, 메시지받기
        services.AddTransient<IBrokerMessagePublisher, RedisMessagePublisher>(); //발송
        break;

    case "sns":
        services.AddSingleton<AwsSQS>(); //신규 큐 생성,구독 기능
        services.AddHostedService<SQSSubscriberService>(); //메시지받기

        services.AddSingleton<AwsSNS>(); //Sns실직적 발송처리
        services.AddTransient<IBrokerMessagePublisher, SnsMessagePublisher>(); //발송(중계역활)
        break;

    default:
        throw new NotImplementedException($"지원하지 않는 BrokerType입니다: '{_brokerType}', sns 혹은 redis 로 설정해야합니다.");
}

//구독된채널 메시지 공통처리
services.AddTransient<BrokerMessageDispatcher>();


#endregion

// 서비스 제공자 빌드
var provider = services.BuildServiceProvider();

Console.WriteLine($"[{runtimeEnv}]Starting the application......");

try
{
    

    Console.WriteLine("App 데이터 초기화 완료.");
    //Environment.Exit(0); // 0은 정상 종료
}
catch (Exception ex)
{
    Console.WriteLine($"App 시작 중 초기화 오류 발생. Exception: {ex}");
    Environment.Exit(1);
}
finally
{
    Console.WriteLine("작업 완료됨.");
}









