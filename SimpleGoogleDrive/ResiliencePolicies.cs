using System.Net;
using System.Reflection;
using Polly;

namespace SimpleGoogleDrive;

internal class ResiliencePolicies
{
    private static int _attempts = 5;
    private static Random _random = new();
    
    public static IAsyncPolicy GetExponentialRetryPolicy() =>
        Policy
            .Handle<Google.GoogleApiException>(t=> t.HttpStatusCode switch
            {
                HttpStatusCode.TooManyRequests => true,
                HttpStatusCode.RequestTimeout => true,
                HttpStatusCode.InternalServerError => true,
                HttpStatusCode.BadGateway => true,
                HttpStatusCode.ServiceUnavailable => true,
                HttpStatusCode.GatewayTimeout => true,
                HttpStatusCode.VariantAlsoNegotiates => true,
                HttpStatusCode.InsufficientStorage => true,
                HttpStatusCode.LoopDetected => true,
                HttpStatusCode.NotExtended => true,
                _ => false
            })
            .WaitAndRetryAsync(
                _attempts,
                i => TimeSpan.FromMilliseconds(_random.Next(200)) + TimeSpan.FromMilliseconds(200 * Math.Pow(2, i))
            );
}