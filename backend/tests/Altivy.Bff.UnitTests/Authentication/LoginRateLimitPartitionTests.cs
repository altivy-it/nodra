using System.Net;
using Altivy.Bff.Authentication;

namespace Altivy.Bff.UnitTests.Authentication;

[Trait("Category", "Unit")]
public sealed class LoginRateLimitPartitionTests
{
    [Fact]
    public void GetKeyUsesIndependentRemoteAddresses()
    {
        var first = new DefaultHttpContext();
        first.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");
        var second = new DefaultHttpContext();
        second.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.11");

        var firstKey = LoginRateLimitPartition.GetKey(first);
        var secondKey = LoginRateLimitPartition.GetKey(second);

        Assert.NotEqual(firstKey, secondKey);
    }
}