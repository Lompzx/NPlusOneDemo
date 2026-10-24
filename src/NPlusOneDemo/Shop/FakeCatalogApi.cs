using System.Net;
using System.Net.Http.Json;
using NPlusOneDemo.Diagnostics;

namespace NPlusOneDemo.Shop;

public record CatalogProduct(int Id, string Name, decimal Price);

/// <summary>
/// In-process stand-in for a product catalog service. Answers
/// <c>GET /products/{id}</c> and <c>GET /products?ids=1,2,3</c>.
/// </summary>
public sealed class FakeCatalogApi(
    IReadOnlyDictionary<int, CatalogProduct> products, RoundTripCounter counter, TimeSpan latency)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        counter.OnHttpCall();
        SimulatedNetwork.Wait(latency);

        var uri = request.RequestUri!;
        var segments = uri.AbsolutePath.Trim('/').Split('/');
        object body = segments.Length == 2
            ? products[int.Parse(segments[1])]
            : uri.Query.TrimStart('?').Replace("ids=", "").Split(',').Select(id => products[int.Parse(id)]).ToList();

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) });
    }
}
