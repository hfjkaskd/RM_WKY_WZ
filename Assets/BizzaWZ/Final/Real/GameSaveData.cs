#if BIZZA_REAL_WITHDRAW
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameSaveData : ISaveData
{
    public string withdrawNameInfo;
    public string withdrawPhoneInfo;

    public string accountIdentificationInfo_E;
    public string accountIdentificationInfo_C;
    public string accountIdentificationInfo_P;
    public string accountIdentificationInfo_V;

    public string withdrawCPFInfo;
    public string withdrawEmailInfo;

    public int pixChannelIndex;
    public int todayAdTimes;

    public bool isWithdrawal = false;

}
// private void Test()
// {
//     // 移动端弹出的键盘类型（数字键盘/邮箱键盘等）
//     accountNameInput.keyboardType = TouchScreenKeyboardType.EmailAddress;
//     // 是否隐藏移动端键盘输入显示/软键盘策略
//     accountNameInput.shouldHideMobileInput = true;
//     accountNameInput.shouldHideSoftKeyboard = true;
//
//     // 移动端软键盘状态变化
//     accountNameInput.onTouchScreenKeyboardStatusChanged = new TMP_InputField.TouchScreenKeyboardEvent();
//
//     // 让输入框获得焦点并进入输入状态
//     accountNameInput.ActivateInputField();
//     // 退出输入状态，可选是否清空选区。
//     accountNameInput.DeactivateInputField();
//     // 当前是否处于焦点/输入状态判断。
//     var temp = accountNameInput.isFocused;
// }

/*
 public enum Status
{
/// <summary>
///   <para>The on-screen keyboard is visible.</para>
/// </summary>
Visible,
/// <summary>
///   <para>The user has finished providing input.</para>
/// </summary>
Done,
/// <summary>
///   <para>The on-screen keyboard was canceled.</para>
/// </summary>
Canceled,
/// <summary>
///   <para>The on-screen keyboard has lost focus.</para>
/// </summary>
LostFocus,
}
 */
#endif