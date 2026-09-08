using System.Net.Http.Headers;
using Altivy.Bff.Relay;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Altivy.Bff.UnitTests.Relay;

[Trait("Category", "Unit")]
public sealed class BffProxyRouteBuilderExtensionsTests
{
    [Theory]
    [InlineData("Authorization")]
    [InlineData("Host")]
    [InlineData("Connection")]
    [InlineData("Transfer-Encoding")]
    public void IsHopByHopHeaderRejectsTransportAndIdentityHeaders(string headerName)
    {
        Assert.True(BffProxyRouteBuilderExtensions.IsHopByHopHeader(headerName));
    }

    [Theory]
    [InlineData("Accept")]
    [InlineData("ETag")]
    [InlineData("X-Correlation-Id")]
    public void IsHopByHopHeaderAllowsPublicHeaders(string headerName)
    {
        Assert.False(BffProxyRouteBuilderExtensions.IsHopByHopHeader(headerName));
    }

    [Fact]
    public void BuildDownstreamUriPreservesPathAndQuery()
    {
        var result = BffProxyRouteBuilderExtensions.BuildDownstreamUri(
            new Uri("https://api.example/"),
            "vaults/lab-0001",
            new QueryString("?include=tree"));

        Assert.Equal("https://api.example/api/v1/vaults/lab-0001?include=tree", result.AbsoluteUri);
    }

    [Fact]
    public void MapBffApiProxyAcceptsQueryMethod()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddHttpClient();
        using var app = builder.Build();
        app.MapBffApiProxy();
        var endpoint = Assert.Single(
            ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>(),
            candidate => candidate.RoutePattern.RawText == "/api/v1/{**path}");

        var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();

        Assert.NotNull(methods);
        Assert.Contains("QUERY", methods.HttpMethods);
    }

    [Fact]
    public void CopyRequestHeadersPreservesIfMatchAndRemovesBrowserAuthorization()
    {
        var source = new HeaderDictionary
        {
            ["If-Match"] = "\"version-1\"",
            ["Authorization"] = "Bearer browser-token"
        };
        using var request = new HttpRequestMessage();

        ProxyHeaders.CopyRequestHeaders(source, request);

        Assert.Equal("\"version-1\"", request.Headers.IfMatch.Single().Tag);
        Assert.Null(request.Headers.Authorization);
    }

    [Fact]
    public void CopyResponseHeadersPreservesEtagAndRemovesConnection()
    {
        using var response = new HttpResponseMessage();
        response.Headers.ETag = new EntityTagHeaderValue("\"version-2\"");
        response.Headers.Connection.Add("close");
        using var content = new StringContent("vault");
        var destination = new HeaderDictionary();

        ProxyHeaders.CopyResponseHeaders(response.Headers, content.Headers, destination);

        Assert.Equal("\"version-2\"", destination["ETag"].ToString());
        Assert.False(destination.ContainsKey("Connection"));
    }

    [Fact]
    public void CopyRequestHeadersPreservesIfNoneMatchAndCustomHeaders()
    {
        var source = new HeaderDictionary
        {
            ["If-None-Match"] = "\"etag-current\"",
            ["X-Custom-Trace"] = "trace-12345",
            ["Proxy-Authorization"] = "Basic dXNlcjpwYXNz"
        };
        using var request = new HttpRequestMessage();

        ProxyHeaders.CopyRequestHeaders(source, request);

        Assert.Equal("\"etag-current\"", request.Headers.IfNoneMatch.Single().Tag);
        Assert.Equal("trace-12345", request.Headers.GetValues("X-Custom-Trace").Single());
        Assert.False(request.Headers.Contains("Proxy-Authorization"));
    }

    [Fact]
    public void CopyResponseHeadersPreservesLastModifiedCacheControlAndContentType()
    {
        using var response = new HttpResponseMessage();
        response.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, MustRevalidate = true };
        using var content = new StringContent("{\"notes\":[]}", System.Text.Encoding.UTF8, "application/json");
        content.Headers.LastModified = DateTimeOffset.UtcNow;
        var destination = new HeaderDictionary();

        ProxyHeaders.CopyResponseHeaders(response.Headers, content.Headers, destination);

        Assert.Contains("no-cache", destination["Cache-Control"].ToString());
        Assert.Contains("application/json", destination["Content-Type"].ToString());
        Assert.True(destination.ContainsKey("Last-Modified"));
    }
}
