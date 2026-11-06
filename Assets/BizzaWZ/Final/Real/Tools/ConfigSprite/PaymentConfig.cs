#if BIZZA_REAL_WITHDRAW
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Obfuz.ObfuzIgnore]
[CreateAssetMenu(
    fileName = "PaymentConfig",
    menuName = "Config/PaymentConfig"
)]
public class PaymentConfig : ScriptableObject
{
    public List<PaymentData> PaymentDatas;

    public Sprite GetSpriteByPayKey(string paymentKey, bool matchCase = true) // 是否考虑大小写
    {
        foreach (var item in PaymentDatas)
        {
            bool isMatch = string.Equals(paymentKey, item.paymentKey) || string.Equals(paymentKey, item.paymentKey, StringComparison.OrdinalIgnoreCase);
            if (isMatch)
            {
                return item.paymentIcon;
            }
        }
        LogLogger.LogVerbose(LogTag.LOG_Asset,"PayeeAccountKey not found " + paymentKey);
        return null;
    }
    
    public Sprite GetSpriteByIconKey(string payIconKey, bool matchCase = true) // 是否考虑大小写
    {
        foreach (var item in PaymentDatas)
        {
            bool isMatch = string.Equals(payIconKey, item.payIconKey) || string.Equals(payIconKey, item.payIconKey, StringComparison.OrdinalIgnoreCase);
            if (isMatch)
            {
                return item.paymentIcon;
            }
        }
        LogLogger.LogVerbose(LogTag.LOG_Asset,"PayeeAccountKey not found " + payIconKey);
        return null;
    }
    
    public Sprite GetSpriteByType(E_PayeeAccountType type, bool matchCase = true) // 是否考虑大小写
    {
        foreach (var item in PaymentDatas)
        {
            if (item.payeeAccountType == type)
            {
                return item.paymentIcon;
            }
        }
        LogLogger.LogVerbose(LogTag.LOG_Asset,"PayeeAccountKey not found " + type);
        return null;
    } 
    
    public Sprite GetSmallSpriteByType(E_PayeeAccountType type) // 是否考虑大小写
    {
        foreach (var item in PaymentDatas)
        {
            if (item.payeeAccountType == type)
            {
                return item.smallPaymentIcon;
            }
        }
        LogLogger.LogVerbose(LogTag.LOG_Asset,"smallPaymentIcon not found " + type);
        return null;
    } 
}

[Serializable][Obfuz.ObfuzIgnore]
public class PaymentData
{
    public string paymentKey; // 服务器返回的key
    public string payIconKey;
    public E_PayeeAccountType payeeAccountType;
    public Sprite paymentIcon;
    public Sprite smallPaymentIcon;
}
#endif