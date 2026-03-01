using System.Security.Cryptography;
using System.Text;

namespace APIseverino.Helpers
{
    public class PasswordHelper
    {
        public static void CriarHashSenha(string senha, out byte[] hash, out byte[] salt)
        {
            using var hmac = new HMACSHA512();
            salt = hmac.Key;
            hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(senha));
        }

        public static bool VerificarSenha(string senha, byte[] hash, byte[] salt)
        {
            using var hmac = new HMACSHA512(salt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(senha));
            return computedHash.SequenceEqual(hash);
        }
    }
}