﻿using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace XSLibrary.Cryptography.ConnectionCryptos
{
    public class RSACrypto : IConnectionCrypto
    {
        
        RSACng KEXCrypto;
        AesCryptoServiceProvider DataCrypto;

        byte[] SECRET = Encoding.ASCII.GetBytes("this. is. RSARTA!");

        public RSACrypto(bool active) : base(active)
        {
            KEXCrypto = new RSACng(2048);

            DataCrypto = new AesCryptoServiceProvider();
            DataCrypto.Padding = PaddingMode.PKCS7;
        }

        protected override bool HandshakeActive(SendCall Send, ReceiveCall Receive)
        {
            Send(Encoding.ASCII.GetBytes(KEXCrypto.ToXmlString(false)));
            if (!Receive(out byte[] data, out EndPoint source))
                return false;

            DataCrypto.Key = KEXCrypto.Decrypt(data, RSAEncryptionPadding.OaepSHA256);
            Send(DataCrypto.IV);

            if (!Receive(out data, out source))
                return false;

            return DecryptSecret(data);
        }

        private bool DecryptSecret(byte[] data)
        {
            byte[] decrypt = DecryptData(data);

            for (int i = 0; i < SECRET.Length && i < decrypt.Length; i++)
            {
                if (SECRET[i] != decrypt[i])
                    return false;
            }

            return true;
        }

        protected override bool HandshakePassive(SendCall Send, ReceiveCall Receive)
        {
            if (!Receive(out byte[] data, out EndPoint source))
                return false;

            KEXCrypto.FromXmlString(Encoding.ASCII.GetString(data));


            byte[] key = new byte[32];
            RandomNumberGenerator.Create().GetBytes(key);

            DataCrypto.Key = key;
            Send(KEXCrypto.Encrypt(key, RSAEncryptionPadding.OaepSHA256));

            if (!Receive(out data, out source))
                return false;

            DataCrypto.IV = data;
            Send(EncryptData(SECRET));

            return true;
        }

        public override byte[] EncryptData(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(stream, DataCrypto.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.Close();
                    return stream.ToArray();
                }
            }
        }

        public override byte[] DecryptData(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(stream, DataCrypto.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.Close();
                    return stream.ToArray();
                }
            }
        }
    }
}
