using System.Security.Claims;
using Nodra.Bff.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace Nodra.Bff.UnitTests.Authentication;

[Trait("Category", "Unit")]
public sealed class ServerSideTicketStoreTests
{
    [Fact]
    public async Task StoreAndRetrieveAsyncPreservesAuthenticationTicketOnServer()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new ServerSideTicketStore(cache);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "synthetic-user")],
            "test"));
        var ticket = new AuthenticationTicket(
            principal,
            new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(5) },
            "test");

        var key = await store.StoreAsync(ticket);
        var retrieved = await store.RetrieveAsync(key);

        Assert.NotNull(retrieved);
        Assert.Equal("synthetic-user", retrieved.Principal.Identity?.Name);
        Assert.DoesNotContain("synthetic-user", key, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RemoveAsyncRemovesAuthenticationTicket()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new ServerSideTicketStore(cache);
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity()),
            "test");
        var key = await store.StoreAsync(ticket);

        await store.RemoveAsync(key);

        Assert.Null(await store.RetrieveAsync(key));
    }

    [Fact]
    public async Task RenewAsyncAfterRemoveDoesNotRestoreTicket()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new ServerSideTicketStore(cache);
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity()),
            "test");
        var key = await store.StoreAsync(ticket);

        await store.RemoveAsync(key);
        await store.RenewAsync(key, ticket);

        Assert.Null(await store.RetrieveAsync(key));
    }
}