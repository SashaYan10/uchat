using System.Security.Cryptography;
using System.Text;

namespace uchat.Services.AuthService
{
    public static class AesCryptography
    {
        private static readonly string _key = "abcdefghijklmnop";

        public static string EncryptPassword(string password)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = new byte[16];

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using MemoryStream ms = new();
            using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
            {
                using StreamWriter sw = new(cs);
                sw.Write(password);
            }

            byte[]? encryptedData = ms.ToArray();

            return Convert.ToBase64String(encryptedData);
        }

        public static string DecryptPassword(string password)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = new byte[16];

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using MemoryStream ms = new(Convert.FromBase64String(password));
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);

            return sr.ReadToEnd();
        }
    }
}
