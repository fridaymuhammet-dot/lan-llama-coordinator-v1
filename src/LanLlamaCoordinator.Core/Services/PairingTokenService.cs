using System.Security.Cryptography;
using System.Text;

namespace LanLlamaCoordinator.Core.Services;

/// <summary>
/// In-memory bearer-token store for the prototype. Persist hashes, never raw tokens, in a production implementation.
/// Tokens must only be provisioned and used over an authenticated encrypted channel (for example, TLS).
/// </summary>
public sealed class PairingTokenService
{
    private readonly object _gate = new();
    private readonly Dictionary<string, byte[]> _tokenHashes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Issues or rotates the worker's 256-bit pairing token. The raw token is returned only once.</summary>
    public string IssueToken(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("A device identifier is required.", nameof(deviceId));
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var tokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        lock (_gate)
        {
            _tokenHashes[deviceId] = tokenHash;
        }

        return token;
    }

    public bool ValidateToken(string deviceId, string? candidateToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(candidateToken) || candidateToken.Length > 128)
        {
            return false;
        }

        var candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(candidateToken));
        lock (_gate)
        {
            return _tokenHashes.TryGetValue(deviceId, out var expectedHash)
                && CryptographicOperations.FixedTimeEquals(expectedHash, candidateHash);
        }
    }

    public bool RevokeToken(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return false;
        }

        lock (_gate)
        {
            return _tokenHashes.Remove(deviceId);
        }
    }
}
