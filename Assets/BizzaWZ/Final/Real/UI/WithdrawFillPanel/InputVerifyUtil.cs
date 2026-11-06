#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class InputVerifyUtil
{
    #region CPF

    // ---------------- CPF ----------------
    // CPF 校验位算法（安卓端常用实现）
    public static bool IsValidCPF(string cpfDigits11)
    {
        if (cpfDigits11 == null || cpfDigits11.Length != 11) return false;

        // 拒绝全相同数字：00000000000, 11111111111 ...
        if (cpfDigits11.Distinct().Count() == 1) return false;

        int[] nums = cpfDigits11.Select(c => c - '0').ToArray();

        // 第1位校验
        int sum1 = 0;
        for (int i = 0; i < 9; i++)
            sum1 += nums[i] * (10 - i);

        int mod1 = sum1 % 11;
        int check1 = (mod1 < 2) ? 0 : (11 - mod1);
        if (nums[9] != check1) return false;

        // 第2位校验
        int sum2 = 0;
        for (int i = 0; i < 10; i++)
            sum2 += nums[i] * (11 - i);

        int mod2 = sum2 % 11;
        int check2 = (mod2 < 2) ? 0 : (11 - mod2);
        if (nums[10] != check2) return false;

        return true;
    }

    // ---------------- CNPJ ----------------
    public static bool IsValidCNPJ(string cnpjDigits14)
    {
        if (cnpjDigits14 == null || cnpjDigits14.Length != 14) return false;

        // 拒绝全相同数字
        if (cnpjDigits14.Distinct().Count() == 1) return false;

        int[] nums = cnpjDigits14.Select(c => c - '0').ToArray();

        // 权重
        int[] w1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] w2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        // 第1位校验
        int sum1 = 0;
        for (int i = 0; i < 12; i++)
            sum1 += nums[i] * w1[i];

        int mod1 = sum1 % 11;
        int check1 = (mod1 < 2) ? 0 : (11 - mod1);
        if (nums[12] != check1) return false;

        // 第2位校验
        int sum2 = 0;
        for (int i = 0; i < 13; i++)
            sum2 += nums[i] * w2[i];

        int mod2 = sum2 % 11;
        int check2 = (mod2 < 2) ? 0 : (11 - mod2);
        if (nums[13] != check2) return false;

        return true;
    }

    #endregion
    
    
}
#endif