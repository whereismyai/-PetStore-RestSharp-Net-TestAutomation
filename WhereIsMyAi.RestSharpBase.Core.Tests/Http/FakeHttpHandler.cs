using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WhereIsMyAi.RestSharpBase.Core.Tests.Http;

/// <summary>Records the last request as it went on the wire and answers with a fixed response, or throws (transport failure).</summary>
public sealed class FakeHttpHandler(Func<HttpResponseMessage>? respond = null) : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }
    public string? RequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        RequestBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return respond?.Invoke() ?? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
    }
}
