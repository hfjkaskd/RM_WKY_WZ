using System;
using System.Text;

public static class RemoteGroupConfigCrypto
{
    private const string Header = "RGCFG1:";
    private const string Key = "RemoteGroup.RuntimeConfig.v1";

    public static string EncryptToText(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText ?? string.Empty);
        Apply(bytes);
        return Header + Convert.ToBase64String(bytes);
    }

    public static bool TryDecryptToText(string encryptedText, out string plainText)
    {
        plainText = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
            {
                return false;
            }

            var trimmedText = RemoteTextUtility.NormalizeText(encryptedText).Trim();
            if (!trimmedText.StartsWith(Header, StringComparison.Ordinal))
            {
                return false;
            }

            var payload = trimmedText.Substring(Header.Length);
            var bytes = Convert.FromBase64String(payload);
            Apply(bytes);
            plainText = Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch
        {
            plainText = string.Empty;
            return false;
        }
    }

    private static void Apply(byte[] bytes)
    {
        if (bytes == null || bytes.Length <= 0)
        {
            return;
        }

        var keyBytes = Encoding.UTF8.GetBytes(Key);
        for (var i = 0; i < bytes.Length; i++)
        {
            var mixedKey = keyBytes[(i * 17 + 7) % keyBytes.Length];
            bytes[i] = (byte)(bytes[i] ^ mixedKey ^ (byte)(i * 31));
        }
    }
}
