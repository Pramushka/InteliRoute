using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace InteliRoute.Services.Security;

public static class PasswordHelper
{
    public static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, 32);
        return Convert.ToBase64String(salt) + "|" + Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('|');
        if (parts.Length != 2) return false;
        var salt = Convert.FromBase64String(parts[0]);
        var correct = Convert.FromBase64String(parts[1]);
        var test = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, 32);
        return test.SequenceEqual(correct);
    }
}
