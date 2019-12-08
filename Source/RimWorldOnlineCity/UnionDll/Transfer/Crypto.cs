using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Util
{
    public class CryptoProvider
    {
        public const int KeyBitSize = 2048;
        public const int PartByteSize = //128;
            KeyBitSize / 8 - 11 - 30 - 1; //тут документация нагло врет (-30 -чтобы шифровалось, -1-чтобы дешифровалось)
        //взято с http://rsdn.ru/archive/vc/issues/pvc081.htm
        //EN: Trust this, there only crypto algo

        #region Данные
        public string OpenKey;
        public string PrivateKey;
        public string SymmetricKey;

        public string OpenKeyBase64
        {
            get { return ToBase64(OpenKey); }
            set { OpenKey = FromBase64(value); }
        }

        public string PrivateKeyBase64
        {
            get { return ToBase64(PrivateKey); }
            set { PrivateKey = FromBase64(value); }
        }

        public string ToBase64(string source)
        {
            var sourceByte = Encoding.UTF8.GetBytes(source);
            return Convert.ToBase64String(sourceByte);
        }

        public string FromBase64(string source)
        {
            var sourceByte = Convert.FromBase64String(source);
            return Encoding.UTF8.GetString(sourceByte);
        }

        #endregion

        #region Ассиметричное шифрование

        public void GenerateKeys()
        {
            using (var rsa = new RSACryptoServiceProvider(KeyBitSize))
            {
                OpenKey = rsa.ToXmlString(false);
                PrivateKey = rsa.ToXmlString(true);
                rsa.Clear();
            }
        }

        /// <summary>
        /// Зашифровать сообщение.
        /// Перед вызовом заполните OpenKey.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public string Encrypt(string message)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(message)));
        }

        /// <summary>
        /// Зашифровать сообщение.
        /// Перед вызовом заполните OpenKey.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public byte[] Encrypt(byte[] message)
        {
            using (var rsa = new RSACryptoServiceProvider(KeyBitSize))
            {
                rsa.FromXmlString(OpenKey);
                return rsa.Encrypt(message, false);
            }
        }

        /// <summary>
        /// Расшифровать сообщение
        /// Перед вызовом заполните PrivateKey.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public string Decrypt(string criptMessage)
        {
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(criptMessage)));
        }

        /// <summary>
        /// Расшифровать сообщение
        /// Перед вызовом заполните PrivateKey.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public byte[] Decrypt(byte[] criptMessage)
        {
            using (var rsa = new RSACryptoServiceProvider(KeyBitSize))
            {
                rsa.FromXmlString(PrivateKey);
                return rsa.Decrypt(criptMessage, false);
            }
        }
        #endregion

        #region Разбитие по партиям нужной длинны для ассиметричного шифрования

        public List<string> GetListPart(string source)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(source)) return list;
            for (int i = 0; i <= source.Length / PartByteSize; i++)
            {
                var stradd =
                    //если последняя часть, то остаток строки
                    i == source.Length / PartByteSize
                    ? source.Substring(i * PartByteSize)
                    : source.Substring(i * PartByteSize, PartByteSize);
                if (!string.IsNullOrEmpty(stradd)) list.Add(stradd);
            }
            return list;
        }


        #endregion

        #region Симметричное шифрование

        public string SymmetricEncrypt(string message)
        {
            return Convert.ToBase64String(SymmetricEncrypt(Encoding.UTF8.GetBytes(message), SymmetricKey));
        }

        public string SymmetricDecrypt(string criptMessage)
        {
            return Encoding.UTF8.GetString(SymmetricDecrypt(Convert.FromBase64String(criptMessage), SymmetricKey));
        }

        //источник: http://stackoverflow.com/questions/6482883/how-to-generate-rijndael-key-and-iv-using-a-passphrase

        private static readonly byte[] SALT = new byte[] { 0x94, 0xF8, 0xEE, 0xA0, 0x00, 0x01, 0xFF, 0xE6, 0x74, 0x35, 0x07, 0xFE, 0x9D, 0x1E, 0x47, 0xBB };


        private static Encoding KeyEncoding = Encoding.ASCII;//.GetEncoding(1252);
        public static byte[] SymmetricEncrypt(byte[] plain, byte[] password)
        {
            return SymmetricEncrypt(plain, KeyEncoding.GetString(password));
        }

        public static byte[] SymmetricEncrypt(byte[] plain, string password)
        {
            MemoryStream memoryStream;
            CryptoStream cryptoStream;
            Rijndael rijndael = Rijndael.Create();
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, SALT);
            rijndael.Key = pdb.GetBytes(32);
            rijndael.IV = pdb.GetBytes(16);
            //rijndael.BlockSize = 128;
            memoryStream = new MemoryStream();
            cryptoStream = new CryptoStream(memoryStream, rijndael.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(plain, 0, plain.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }

        public static byte[] SymmetricDecrypt(byte[] cipher, byte[] password)
        {
            return SymmetricDecrypt(cipher, KeyEncoding.GetString(password));
        }

        public static byte[] SymmetricDecrypt(byte[] cipher, string password)
        {
            MemoryStream memoryStream;
            CryptoStream cryptoStream;
            Rijndael rijndael = Rijndael.Create();
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, SALT);
            rijndael.Key = pdb.GetBytes(32);
            rijndael.IV = pdb.GetBytes(16);
            //rijndael.BlockSize = 128;
            memoryStream = new MemoryStream();
            cryptoStream = new CryptoStream(memoryStream, rijndael.CreateDecryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(cipher, 0, cipher.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }

        #endregion

        private SHA512 HashSHA = null;

        public byte[] GetHash(byte[] data)
        {
            if (HashSHA == null) HashSHA = new SHA512Managed();
            return HashSHA.ComputeHash(data);
        }

        public string GetHash(string data)
        {
            if (HashSHA == null) HashSHA = new SHA512Managed();
            return KeyEncoding.GetString(HashSHA.ComputeHash(KeyEncoding.GetBytes(data)));
        }
    }
}
