using System.Net;
using System.Net.Http.Json;

namespace AcmeCoffeeOnline.Web.Tests.Infrastructure;

public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    public List<SentRequest> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(new SentRequest(
            request.Method,
            request.RequestUri?.PathAndQuery ?? string.Empty));

        return await _handler(request, cancellationToken);
    }

    public static HttpResponseMessage Json(HttpStatusCode statusCode, object? value)
        => new(statusCode)
        {
            Content = JsonContent.Create(value)
        };

    public static HttpResponseMessage Empty(HttpStatusCode statusCode)
        => new(statusCode);
}

public sealed record SentRequest(HttpMethod Method, string PathAndQuery);
