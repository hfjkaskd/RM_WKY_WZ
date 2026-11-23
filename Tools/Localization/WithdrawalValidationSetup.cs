#if UNITY_EDITOR && BIZZA_REAL_WITHDRAW
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Temporary preview-scene fixture only. Copy into the isolated validation
// project's Editor folder, never into the game's runtime assembly.
public static class WithdrawalValidationSetup
{
    private static cfg.TblLanguage table;

    public static void Configure(GameObject root, string language)
    {
        if (!root.TryGetComponent<UIWithdrawalPanel>(out var fill)) return;
        bool brazil = language == "pt-BR";
        bool indonesia = language == "id-ID";

        // Mirror the visible profile selection in OnRefresh without invoking
        // OnOpen, save data, event registration, account state, or any SDK call.
        SetList(fill.PIXObjList, false);
        SetList(fill.PagBankList, false);
        SetList(fill.PaypalList, false);
        SetList(fill.OVOList, false);
        SetList(brazil ? fill.PagBankList : indonesia ? fill.OVOList : fill.PaypalList, true);
        SetList(fill.ErrorList, false);
        Set(fill.FillingRoot, true);
        Set(fill.PlatformRoot, false);
        Set(fill.PlatformIconRoot, true);
        Set(fill.ResultRoot, false);

        var serialized = new SerializedObject(fill);
        SetAuthoredObject(serialized, "accountNameTitle", !brazil);
        SetAuthoredObject(serialized, "cpfNumberTitle", !brazil);
        SetAuthoredObject(serialized, "pixAccountTitle", !brazil);
        SetAuthoredObject(serialized, "brazilAccountNameTitle", brazil);
        SetAuthoredObject(serialized, "brazilCpfNumberTitle", brazil);
        SetAuthoredObject(serialized, "brazilPixAccountTitle", false);

        fill.accountNameInput.PlaceHolderText = Text(brazil
            ? "Withdrawal_NameDocumentPlaceholder" : "UIWithdrawalConfirm_EnterName", language);
        fill.CPFNumberInput.PlaceHolderText = Text("Withdrawal_CpfPlaceholder", language);
        fill.accPhoneMailInput.PlaceHolderText = Text("Withdrawal_IndonesiaAccountPlaceholder", language);
        if (brazil)
            fill.paypalMailTitle.text = Text("UIWithdrawalConfirm_AccountNumber", language);
        else if (!indonesia)
            fill.paypalMailInput.PlaceHolderText = Text("RM_WithdrawalPayPal_Placeholder", language);

        if (fill.paymentImage != null && fill.paymentList != null)
            fill.paymentImage.sprite = fill.paymentList.GetSpriteByType(brazil
                ? E_PayeeAccountType.Pagbank : indonesia ? E_PayeeAccountType.Dana : E_PayeeAccountType.Paypal);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(fill.InputRoot);
        fill.BG.sizeDelta = new Vector2(fill.BG.sizeDelta.x, fill.defaultHeight + fill.InputRoot.rect.height);
        Canvas.ForceUpdateCanvases();
    }

    private static void SetAuthoredObject(SerializedObject serialized, string property, bool visible)
    {
        var value = serialized.FindProperty(property)?.objectReferenceValue as GameObject;
        if (value == null) throw new InvalidDataException("Missing withdrawal preview binding: " + property);
        value.SetActive(visible);
    }

    private static void Set(GameObject value, bool visible)
    {
        if (value != null) value.SetActive(visible);
    }

    private static void SetList(IEnumerable<GameObject> values, bool visible)
    {
        if (values == null) return;
        foreach (var value in values) Set(value, visible);
    }

    private static string Text(string key, string language)
    {
        if (table == null)
        {
            table = new cfg.TblLanguage();
            table.Init(File.ReadAllBytes("Assets/Game/Resources/ConfigAssets/TableBin/Table01/tbllanguage.bytes"));
        }
        var row = table.GetOrDefault(key);
        if (row == null || !row.Dict.TryGetValue(language, out string text) || string.IsNullOrEmpty(text))
            throw new InvalidDataException("Missing withdrawal preview translation: " + key + "/" + language);
        return text;
    }
}
#endif
