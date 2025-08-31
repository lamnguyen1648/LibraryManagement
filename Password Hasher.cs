using System;
using System.Security.Cryptography;

namespace LibraryManagement
{
    public static class PasswordHasher
    {
        private const int DefaultIterations = 200_000;
        private const int SaltSize = 16;  // 128-bit
        private const int KeySize  = 32;  // 256-bit

        public static string HashPassword(string password, int? iterationsOverride = null)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            int iterations = iterationsOverride ?? DefaultIterations;

            Span<byte> salt = stackalloc byte[SaltSize];
            RandomNumberGenerator.Fill(salt);

            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                password: password,
                salt: salt,
                iterations: iterations,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: KeySize
            );

            return $"pbkdf2-sha256.{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string encodedHash, out bool needsRehash)
        {
            needsRehash = false;
            if (password == null || string.IsNullOrWhiteSpace(encodedHash)) return false;

            var parts = encodedHash.Split('.');
            if (parts.Length != 4 || parts[0] != "pbkdf2-sha256") return false;

            if (!int.TryParse(parts[1], out int iterations)) return false;
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expectedKey = Convert.FromBase64String(parts[3]);

            byte[] actualKey = Rfc2898DeriveBytes.Pbkdf2(
                password: password,
                salt: salt,
                iterations: iterations,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: expectedKey.Length
            );

            bool ok = CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);

            if (ok && iterations < DefaultIterations)
                needsRehash = true;

            return ok;
        }
    }
}
