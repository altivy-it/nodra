using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Nodra.Bff.Authentication;

/// <summary>
/// Armazena tickets de autenticação no cache do servidor em vez de colocá-los no cookie.
/// </summary>
public sealed class ServerSideTicketStore(IMemoryCache cache) : ITicketStore
{
    private static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(8);

    /// <summary>
    /// Persiste um ticket e gera uma chave opaca para referenciá-lo.
    /// </summary>
    /// <param name="ticket">Ticket de autenticação a armazenar.</param>
    /// <returns>Chave que identifica o ticket armazenado.</returns>
    public Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        SaveToCache(key, new TicketEntry(ticket), ticket);
        return Task.FromResult(key);
    }

    /// <summary>
    /// Atualiza um ticket existente e renova sua expiração no cache.
    /// </summary>
    /// <param name="key">Chave do ticket existente.</param>
    /// <param name="ticket">Novo conteúdo do ticket.</param>
    /// <returns>Uma tarefa que representa a atualização.</returns>
    public Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        if (cache.TryGetValue(key, out TicketEntry? entry) && entry is not null)
        {
            lock (entry.Gate)
            {
                if (!entry.Removed)
                {
                    entry.Ticket = ticket;
                    SaveToCache(key, entry, ticket);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Recupera um ticket ativo do cache do servidor.
    /// </summary>
    /// <param name="key">Chave do ticket a recuperar.</param>
    /// <returns>O ticket ativo ou <c>null</c> quando ele não existe ou foi removido.</returns>
    public Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var entry = cache.Get<TicketEntry>(key);
        if (entry is null)
        {
            return Task.FromResult<AuthenticationTicket?>(null);
        }

        lock (entry.Gate)
        {
            return Task.FromResult(entry.Removed ? null : entry.Ticket);
        }
    }

    /// <summary>
    /// Invalida um ticket armazenado no servidor.
    /// </summary>
    /// <param name="key">Chave do ticket a remover.</param>
    /// <returns>Uma tarefa que representa a remoção lógica.</returns>
    public Task RemoveAsync(string key)
    {
        if (cache.TryGetValue(key, out TicketEntry? entry) && entry is not null)
        {
            lock (entry.Gate)
            {
                entry.Removed = true;
                entry.Ticket = null;
                cache.Set(key, entry, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = DefaultLifetime,
                    Size = 1
                });
            }
        }

        return Task.CompletedTask;
    }

    private void SaveToCache(string key, TicketEntry entry, AuthenticationTicket ticket)
    {
        var expiration = ticket.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.Add(DefaultLifetime);
        cache.Set(key, entry, new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expiration,
            Size = 1
        });
    }

    private sealed class TicketEntry(AuthenticationTicket ticket)
    {
        public object Gate { get; } = new();

        public AuthenticationTicket? Ticket { get; set; } = ticket;

        public bool Removed { get; set; }
    }
}