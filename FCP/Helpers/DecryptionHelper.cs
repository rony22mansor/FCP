using System;
using System.IO;
using System.Security.Cryptography;

public static class DecryptionHelper
{
    public static byte[] DecryptWithPassword(byte[] encryptedData, string password)
    {
        if (encryptedData.Length < 32)
        {
            throw new ArgumentException("The encrypted data is too short to contain salt and IV.");
        }

        // استخراج الـ Salt والـ IV من بداية البيانات
        byte[] salt = new byte[16];
        byte[] iv = new byte[16];
        Buffer.BlockCopy(encryptedData, 0, salt, 0, salt.Length);
        Buffer.BlockCopy(encryptedData, salt.Length, iv, 0, iv.Length);

        // باقي البيانات المشفرة
        int encryptedContentOffset = salt.Length + iv.Length;
        int encryptedContentLength = encryptedData.Length - encryptedContentOffset;
        byte[] cipherBytes = new byte[encryptedContentLength];
        Buffer.BlockCopy(encryptedData, encryptedContentOffset, cipherBytes, 0, encryptedContentLength);

        using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100000))
        using (Aes aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.Key = deriveBytes.GetBytes(32);
            aes.IV = iv;

            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(cipherBytes, 0, cipherBytes.Length);
                cs.FlushFinalBlock();
                return ms.ToArray();
            }
        }
    }
}
