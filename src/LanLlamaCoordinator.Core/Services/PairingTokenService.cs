using System.Security.Cryptography;
using System.Text;

namespace LanLlamaCoordinator.Core.Services;

public sealed class PairingTokenService
{
    private readonly Dictionary<string, string> _pairingTokens = new(StringComparer.OrdinalIgnoreCase);

    public string IssueToken(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("A device identifier is required.", nameof(deviceId));
        }

        var random = RandomNumberGenerator.GetBytes(32);
        var secret = Convert.ToHexString(random);
        var token = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{deviceId}:{secret}")));

        _pairingTokens[deviceId] = token;
        return token;
    }

    public bool ValidateToken(string deviceId, string? candidateToken)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(candidateToken))
        {
            return false;
        }

        return _pairingTokens.TryGetValue(deviceId, out var expected)
            && string.Equals(expected, candidateToken, StringComparison.Ordinal);
    }
}
