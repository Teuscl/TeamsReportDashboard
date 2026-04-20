namespace TeamsReportDashboard.Tests.Fakes;

/// <summary>
/// HttpMessageHandler que retorna respostas pré-configuradas para testes unitários.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public FakeHttpMessageHandler(HttpResponseMessage response)
        : this(_ => Task.FromResult(response)) { }

    public FakeHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        => _handler = handler;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => _handler(request);
}
