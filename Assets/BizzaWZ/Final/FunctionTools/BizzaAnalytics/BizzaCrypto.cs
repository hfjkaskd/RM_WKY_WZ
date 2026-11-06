using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

internal sealed class BizzaCrypto
{
  private const int RequiredKeyBytes = 32;
  private const int IvBytes = 12;
  private const int BlockBytes = 16;
  private const int TagBytes = 16;
  private const string EnvelopeVersion = "enc-v1";
  private const string EnvelopeAlgorithm = "A256GCM";
  private const ulong GaloisRHi = 0xE100000000000000UL;

  private readonly byte[] _keyBytes;
  private readonly string _kid;
  private readonly Aes _aes;
  private readonly ICryptoTransform _encryptor;
  private readonly object _encryptLock = new object();
  private readonly ulong _hashSubkeyHi;
  private readonly ulong _hashSubkeyLo;

  private BizzaCrypto(byte[] keyBytes, string kid)
  {
    _keyBytes = keyBytes;
    _kid = kid;

    _aes = new AesManaged
    {
      KeySize = RequiredKeyBytes * 8,
      BlockSize = BlockBytes * 8,
      Mode = CipherMode.ECB,
      Padding = PaddingMode.None,
      Key = _keyBytes,
    };
    _encryptor = _aes.CreateEncryptor();

    var hashSubkey = new byte[BlockBytes];
    EncryptBlock(new byte[BlockBytes], 0, hashSubkey, 0);
    _hashSubkeyHi = ReadUInt64BE(hashSubkey, 0);
    _hashSubkeyLo = ReadUInt64BE(hashSubkey, 8);
  }

  public static bool TryCreate(string keyBase64, string kid, out BizzaCrypto crypto, out string error)
  {
    crypto = null;
    error = "";

    if (string.IsNullOrWhiteSpace(keyBase64))
    {
      error = "encryption_key_base64_is_empty";
      return false;
    }

    byte[] rawKey;
    try
    {
      rawKey = Convert.FromBase64String(keyBase64.Trim());
    }
    catch (Exception ex)
    {
      error = "encryption_key_base64_invalid:" + ex.Message;
      return false;
    }

    if (rawKey.Length != RequiredKeyBytes)
    {
      error = "encryption_key_must_be_32_bytes";
      return false;
    }

    var normalizedKid = string.IsNullOrWhiteSpace(kid) ? "sdk-default-kid" : kid.Trim();
    var stableKey = new byte[rawKey.Length];
    Buffer.BlockCopy(rawKey, 0, stableKey, 0, rawKey.Length);

    crypto = new BizzaCrypto(stableKey, normalizedKid);
    return true;
  }

  public Dictionary<string, object> EncryptValue(object value)
  {
    var normalized = BizzaValueUtil.Normalize(value);
    var plainJson = BizzaJson.Serialize(normalized);
    var plainBytes = Encoding.UTF8.GetBytes(plainJson);

    var iv = new byte[IvBytes];
    using (var rng = RandomNumberGenerator.Create())
    {
      rng.GetBytes(iv);
    }

    var ciphertext = new byte[plainBytes.Length];
    var tag = new byte[TagBytes];
    EncryptAesGcm(iv, plainBytes, ciphertext, tag);

    var encPayload = new Dictionary<string, object>(6);
    encPayload["v"] = EnvelopeVersion;
    encPayload["alg"] = EnvelopeAlgorithm;
    encPayload["kid"] = _kid;
    encPayload["iv"] = Convert.ToBase64String(iv);
    encPayload["ct"] = Convert.ToBase64String(ciphertext);
    encPayload["tag"] = Convert.ToBase64String(tag);

    var wrappedPayload = new Dictionary<string, object>(1);
    wrappedPayload["__enc__"] = encPayload;
    return wrappedPayload;
  }

  public object DecryptValue(object wrappedValue)
  {
    IDictionary wrapper = wrappedValue as IDictionary;
    if (wrapper == null || !wrapper.Contains("__enc__"))
    {
      throw new InvalidDataException("加密信封缺少 __enc__ 字段。");
    }

    IDictionary encryptedPayload = wrapper["__enc__"] as IDictionary;
    if (encryptedPayload == null)
    {
      throw new InvalidDataException("加密信封格式无效。");
    }

    string version = ReadEnvelopeString(encryptedPayload, "v");
    string algorithm = ReadEnvelopeString(encryptedPayload, "alg");
    string kid = ReadEnvelopeString(encryptedPayload, "kid");
    if (!string.Equals(version, EnvelopeVersion, StringComparison.Ordinal)
      || !string.Equals(algorithm, EnvelopeAlgorithm, StringComparison.Ordinal)
      || !string.Equals(kid, _kid, StringComparison.Ordinal))
    {
      throw new InvalidDataException("加密信封版本、算法或 kid 不匹配。");
    }

    byte[] iv = ReadEnvelopeBytes(encryptedPayload, "iv");
    byte[] ciphertext = ReadEnvelopeBytes(encryptedPayload, "ct");
    byte[] tag = ReadEnvelopeBytes(encryptedPayload, "tag");
    if (iv.Length != IvBytes || tag.Length != TagBytes)
    {
      throw new InvalidDataException("加密信封 IV 或 tag 长度无效。");
    }

    byte[] expectedTag = new byte[TagBytes];
    byte[] j0 = BuildJ0(iv);
    ComputeTag(j0, ciphertext, expectedTag);
    if (!FixedTimeEquals(expectedTag, tag))
    {
      throw new InvalidDataException("加密配置完整性校验失败。");
    }

    byte[] plaintext = new byte[ciphertext.Length];
    DecryptAesGcm(iv, ciphertext, plaintext);
    string json = Encoding.UTF8.GetString(plaintext);
    object value = BizzaJson.Deserialize(json);
    if (value == null)
    {
      throw new InvalidDataException("解密后的 JSON 为空或格式无效。");
    }

    return value;
  }

  private static string ReadEnvelopeString(IDictionary payload, string key)
  {
    if (!payload.Contains(key) || payload[key] == null)
    {
      throw new InvalidDataException("加密信封缺少字段：" + key + "。");
    }

    string value = payload[key] as string ?? Convert.ToString(payload[key]);
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new InvalidDataException("加密信封字段为空：" + key + "。");
    }
    return value;
  }

  private static byte[] ReadEnvelopeBytes(IDictionary payload, string key)
  {
    string value = ReadEnvelopeString(payload, key);
    try
    {
      return Convert.FromBase64String(value);
    }
    catch (Exception exception)
    {
      throw new InvalidDataException("加密信封字段不是有效 Base64：" + key + "。", exception);
    }
  }

  private void EncryptAesGcm(byte[] iv, byte[] plaintext, byte[] ciphertext, byte[] tag)
  {
    var j0 = BuildJ0(iv);
    var counterBlock = new byte[BlockBytes];
    Buffer.BlockCopy(j0, 0, counterBlock, 0, BlockBytes);
    var streamBlock = new byte[BlockBytes];

    var offset = 0;
    while (offset < plaintext.Length)
    {
      IncrementCounter(counterBlock);
      EncryptBlock(counterBlock, 0, streamBlock, 0);

      var blockLength = Math.Min(BlockBytes, plaintext.Length - offset);
      for (var i = 0; i < blockLength; i++)
      {
        ciphertext[offset + i] = (byte)(plaintext[offset + i] ^ streamBlock[i]);
      }

      offset += blockLength;
    }

    ComputeTag(j0, ciphertext, tag);
  }

  private void DecryptAesGcm(byte[] iv, byte[] ciphertext, byte[] plaintext)
  {
    var counterBlock = BuildJ0(iv);
    var streamBlock = new byte[BlockBytes];

    var offset = 0;
    while (offset < ciphertext.Length)
    {
      IncrementCounter(counterBlock);
      EncryptBlock(counterBlock, 0, streamBlock, 0);

      var blockLength = Math.Min(BlockBytes, ciphertext.Length - offset);
      for (var i = 0; i < blockLength; i++)
      {
        plaintext[offset + i] = (byte)(ciphertext[offset + i] ^ streamBlock[i]);
      }

      offset += blockLength;
    }
  }

  private static byte[] BuildJ0(byte[] iv)
  {
    var j0 = new byte[BlockBytes];
    Buffer.BlockCopy(iv, 0, j0, 0, IvBytes);
    j0[BlockBytes - 1] = 1;
    return j0;
  }

  private static bool FixedTimeEquals(byte[] left, byte[] right)
  {
    if (left == null || right == null || left.Length != right.Length)
    {
      return false;
    }

    var difference = 0;
    for (var i = 0; i < left.Length; i++)
    {
      difference |= left[i] ^ right[i];
    }
    return difference == 0;
  }

  private void ComputeTag(byte[] j0, byte[] ciphertext, byte[] tag)
  {
    var auth = new byte[BlockBytes];
    var offset = 0;

    while (offset < ciphertext.Length)
    {
      var blockLength = Math.Min(BlockBytes, ciphertext.Length - offset);
      XorPartial(auth, ciphertext, offset, blockLength);
      MultiplyByHashSubkey(auth);
      offset += blockLength;
    }

    var lengthBlock = new byte[BlockBytes];
    WriteUInt64BE(lengthBlock, 8, checked((ulong)ciphertext.LongLength * 8UL));
    XorBlock(auth, lengthBlock);
    MultiplyByHashSubkey(auth);

    var encryptedJ0 = new byte[BlockBytes];
    EncryptBlock(j0, 0, encryptedJ0, 0);
    for (var i = 0; i < BlockBytes; i++)
    {
      tag[i] = (byte)(auth[i] ^ encryptedJ0[i]);
    }
  }

  private void MultiplyByHashSubkey(byte[] block)
  {
    var zHi = 0UL;
    var zLo = 0UL;
    var vHi = _hashSubkeyHi;
    var vLo = _hashSubkeyLo;
    var xHi = ReadUInt64BE(block, 0);
    var xLo = ReadUInt64BE(block, 8);

    MultiplyWord(xHi, ref zHi, ref zLo, ref vHi, ref vLo);
    MultiplyWord(xLo, ref zHi, ref zLo, ref vHi, ref vLo);

    WriteUInt64BE(block, 0, zHi);
    WriteUInt64BE(block, 8, zLo);
  }

  private static void MultiplyWord(ulong word, ref ulong zHi, ref ulong zLo, ref ulong vHi, ref ulong vLo)
  {
    for (var bit = 63; bit >= 0; bit--)
    {
      if (((word >> bit) & 1UL) != 0)
      {
        zHi ^= vHi;
        zLo ^= vLo;
      }

      var lsb = (vLo & 1UL) != 0;
      vLo = (vLo >> 1) | (vHi << 63);
      vHi >>= 1;
      if (lsb)
      {
        vHi ^= GaloisRHi;
      }
    }
  }

  private void EncryptBlock(byte[] input, int inputOffset, byte[] output, int outputOffset)
  {
    lock (_encryptLock)
    {
      _encryptor.TransformBlock(input, inputOffset, BlockBytes, output, outputOffset);
    }
  }

  private static void IncrementCounter(byte[] counterBlock)
  {
    for (var i = BlockBytes - 1; i >= BlockBytes - 4; i--)
    {
      counterBlock[i] += 1;
      if (counterBlock[i] != 0)
      {
        break;
      }
    }
  }

  private static void XorPartial(byte[] accumulator, byte[] source, int sourceOffset, int sourceLength)
  {
    for (var i = 0; i < sourceLength; i++)
    {
      accumulator[i] ^= source[sourceOffset + i];
    }
  }

  private static void XorBlock(byte[] left, byte[] right)
  {
    for (var i = 0; i < BlockBytes; i++)
    {
      left[i] ^= right[i];
    }
  }

  private static ulong ReadUInt64BE(byte[] buffer, int offset)
  {
    return
      ((ulong)buffer[offset] << 56) |
      ((ulong)buffer[offset + 1] << 48) |
      ((ulong)buffer[offset + 2] << 40) |
      ((ulong)buffer[offset + 3] << 32) |
      ((ulong)buffer[offset + 4] << 24) |
      ((ulong)buffer[offset + 5] << 16) |
      ((ulong)buffer[offset + 6] << 8) |
      buffer[offset + 7];
  }

  private static void WriteUInt64BE(byte[] buffer, int offset, ulong value)
  {
    buffer[offset] = (byte)(value >> 56);
    buffer[offset + 1] = (byte)(value >> 48);
    buffer[offset + 2] = (byte)(value >> 40);
    buffer[offset + 3] = (byte)(value >> 32);
    buffer[offset + 4] = (byte)(value >> 24);
    buffer[offset + 5] = (byte)(value >> 16);
    buffer[offset + 6] = (byte)(value >> 8);
    buffer[offset + 7] = (byte)value;
  }
}
