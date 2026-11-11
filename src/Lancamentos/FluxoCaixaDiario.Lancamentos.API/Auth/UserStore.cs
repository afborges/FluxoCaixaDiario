using System.Security.Cryptography;
using System.Text;

namespace FluxoCaixaDiario.Lancamentos.API.Auth;

public sealed class UserInfo
{
    public required string Username { get; init; }
    public required string PasswordHash { get; init; }
    public required string Role { get; init; }
    public required string NomeCompleto { get; init; }
}

public sealed class UserStore
{
    private readonly Dictionary<string, UserInfo> _users;

    public UserStore(IConfiguration configuration)
    {
        var list = configuration
            .GetSection("Users")
            .Get<UserInfo[]>();

        _users = list is { Length: > 0 }
            ? list.ToDictionary(u => u.Username.ToLowerInvariant())
            : [];
    }

    public UserInfo? Validar(string username, string password)
    {
        if (!_users.TryGetValue(username.ToLowerInvariant(), out var user))
            return null;

        var hash = ComputarHash(password);
        return string.Equals(hash, user.PasswordHash, StringComparison.OrdinalIgnoreCase)
            ? user
            : null;
    }

    private static string ComputarHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
