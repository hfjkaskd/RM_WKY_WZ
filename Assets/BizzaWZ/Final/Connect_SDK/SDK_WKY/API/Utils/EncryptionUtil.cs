#if BIZZA_REAL_WITHDRAW
using System;
using System.Security.Cryptography;
using System.Text;
using Bizza.Sdk;
using Xxtea;

/// <summary>
/// 加解密功能，专门负责数据的加密和解密工作
/// </summary>
public static class EncryptionUtil
{
    /// <summary>
    /// 加密
    /// </summary>
    /// <param name="aesKey"></param>
    /// <param name="jsonData"></param>
    /// <returns></returns>
    public static string UrlSafeBase64EncodeWithAes(string jsonData)
    {
        string aesKey = ChannelConfig.Instance.httpConfig.aes_key;
        // 加密逻辑
        LogLogger.LogInfo("加密 " + jsonData);
        var k = Encoding.UTF8.GetBytes(aesKey);
        byte[] aesData = AesEncode(jsonData, k);
        return Base64Encode(aesData);
    }
    
    public static string Base64EncodeWithAes(string jsonData)
    {
        LogLogger.LogInfo(jsonData);
        var k = Encoding.UTF8.GetBytes(ChannelConfig.Instance.httpConfig.aes_key);
        byte[] aesData = AesEncode(jsonData, k);
        return Base64Encode(aesData);
    }
    
    public static string UrlSafeBase64Encode(byte[] data)
    {
        string base64EncodedString = Base64Encode(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return base64EncodedString;
    }
    
    public static byte[] AesEncode(string jsonData, byte[] key)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(jsonData);
        byte[] result;
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.ECB; // 设置为ECB模式
            aes.Padding = PaddingMode.PKCS7; // 设置填充模式
            ICryptoTransform encryptor = aes.CreateEncryptor();
            result = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
        }
    
        return result;
    }
    
    public static string Base64Encode(byte[] data)
    {
        string base64EncodedString = Convert.ToBase64String(data);
        return base64EncodedString;
    }
    
    public static string DecryptAes(string ciphertext, byte[] key)
    {
        LogLogger.LogInfo(ciphertext);
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
    
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] cipherTextBytes = Convert.FromBase64String(ciphertext);
            byte[] plainTextBytes = decryptor.TransformFinalBlock(cipherTextBytes, 0, cipherTextBytes.Length);
            return Encoding.UTF8.GetString(plainTextBytes);
        }
    }
    
    public static string MD5Encoding(string text)
    {
        MD5 md5 = MD5.Create();
        byte[] inputBytes = Encoding.ASCII.GetBytes(text);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
    
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < hashBytes.Length; i++)
        {
            sb.Append(hashBytes[i].ToString("x2"));
        }
    
        return sb.ToString();
    }
    
    // 加密
    public static Byte[] EncryptByXXTeA(string str, string key)
    {
        Byte[] encrypt_data = XXTEA.Encrypt(str, key);
        return encrypt_data;
    }
    
    // 解密
    public static string DecryptByXXTeA(Byte[] encrypt_data, string key)
    {
        String decrypt_data = XXTEA.DecryptToString(encrypt_data, key);
        return decrypt_data;
    }
}

#endif