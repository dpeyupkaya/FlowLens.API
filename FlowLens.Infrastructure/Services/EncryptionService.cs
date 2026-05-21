using System.Security.Cryptography;
using FlowLens.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlowLens.Infrastructure.Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
     
        var keyString = configuration["Encryption:Key"];

        if (string.IsNullOrEmpty(keyString))
            throw new ArgumentNullException("Encryption key is not configured.");

        _key = Convert.FromBase64String(keyString);
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV(); 

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();

        var iv = new byte[16];
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        aes.IV = iv;
        aes.Key = _key;

        var encryptor = aes.CreateDecryptor(aes.Key, aes.IV);

        using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}