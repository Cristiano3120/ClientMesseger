using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClientMesseger
{
#pragma warning disable CS8618
    internal static class Security
    {
        private static RSAParameters _publicServerRSAKey;
        private static Aes _privateAes;

        public static void Initialize()
        {
            GenerateAesKey();
        }

        public static void SaveRSAKey(JsonElement root)
        {
            _publicServerRSAKey.Modulus = root.GetProperty("modulus").GetBytesFromBase64();
            _publicServerRSAKey.Exponent = root.GetProperty("exponent").GetBytesFromBase64();
            SendAesKey();
        }

        #region Aes

        private static void GenerateAesKey()
        {
            _privateAes = Aes.Create();
            _privateAes.GenerateKey();
            _privateAes.GenerateIV();
        }

        private static void SendAesKey()
        {
            var Key = Convert.ToBase64String(_privateAes.Key);
            var IV = Convert.ToBase64String(_privateAes.IV);
            var payload = new
            {
                code = 1,
                Key,
                IV
            };
            var jsonString = JsonSerializer.Serialize(payload);
            _ = Client.SendPayloadAsync(jsonString, EncryptionMode.RSA);
        }
        #endregion

        #region Encryption

        public static byte[] EncryptDataAes(byte[] data)
        {
            var aes = _privateAes;
            aes.Key = _privateAes.Key;
            aes.IV = _privateAes.IV;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    csEncrypt.Write(data, 0, data.Length);
                    csEncrypt.FlushFinalBlock();
                    return msEncrypt.ToArray();
                }
            }
        }

        public static byte[] EncryptRSAData(byte[] data)
        {
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(_publicServerRSAKey);
                return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
            }
        }
        #endregion

        #region Decryption

        public static JsonElement? DecryptMessage(byte[] buffer)
        {
            var decryptionMode = EncryptionMode.AES;
            while (decryptionMode >= EncryptionMode.None)
            {
                try
                {
                    string data = decryptionMode switch
                    {
                        EncryptionMode.AES => DecryptDataAES(buffer),
                        EncryptionMode.None => Encoding.UTF8.GetString(buffer),
                        _ => throw new NotSupportedException("This encryption mode isnt supported at the moment!")
                    };
                    return JsonDocument.Parse(data).RootElement;
                }
                catch (Exception ex) when (ex is CryptographicException || ex is JsonException)
                {
                    if (decryptionMode > 0)
                    {
                        decryptionMode = decryptionMode - 2;
                        Console.WriteLine($"Couldnt decrypt the data." +
                        $" Trying again with {decryptionMode} decryption");
                    }
                }
            }
            return null;
        }

        private static string DecryptDataAES(byte[] encryptedData)
        {
            var aes = _privateAes;
            aes.Key = _privateAes.Key;
            aes.IV = _privateAes.IV;

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var msDecrypt = new System.IO.MemoryStream(encryptedData))
            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
        #endregion 
    }
}
