﻿using System;
using System.IO;
using System.Security.Cryptography;

public static class EncryptionHelper
{
    public static byte[] EncryptWithPassword(byte[] plainData, string password)
    {
        using (Aes aes = Aes.Create())
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            aes.KeySize = 256;
            aes.GenerateIV();

            byte[] salt = new byte[16];
            rng.GetBytes(salt);

            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, 100000))
            {
                aes.Key = deriveBytes.GetBytes(32);
            }

            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(plainData, 0, plainData.Length);
                cs.FlushFinalBlock();

                byte[] encryptedData = ms.ToArray();
                byte[] result = new byte[salt.Length + aes.IV.Length + encryptedData.Length];

                Buffer.BlockCopy(salt, 0, result, 0, salt.Length);
                Buffer.BlockCopy(aes.IV, 0, result, salt.Length, aes.IV.Length);
                Buffer.BlockCopy(encryptedData, 0, result, salt.Length + aes.IV.Length, encryptedData.Length);

                return result;
            }
        }
    }

    
    public static void HandleEncryptionAndWriteFile(
        byte[] archiveBytes,
        string password,
        bool shouldEncrypt,
        string outputPath)
    {
        byte[] finalData;

        if (shouldEncrypt && !string.IsNullOrWhiteSpace(password))
        {
            byte[] encryptedBytes = EncryptWithPassword(archiveBytes, password);

           
            finalData = new byte[1 + encryptedBytes.Length];
            finalData[0] = (byte)'E';
            Buffer.BlockCopy(encryptedBytes, 0, finalData, 1, encryptedBytes.Length);
        }
        else
        {
           
            finalData = new byte[1 + archiveBytes.Length];
            finalData[0] = (byte)'U';
            Buffer.BlockCopy(archiveBytes, 0, finalData, 1, archiveBytes.Length);
        }

        File.WriteAllBytes(outputPath, finalData);
    }
}
