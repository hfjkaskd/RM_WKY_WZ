using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class PausePanel : UIPageBase
{
    public BizzaButton CloseButton;
    public BizzaButton BackButton;
    public BizzaButton ContinueButton;

    public BizzaButton MainBackButton;

    public GameObject GamePauseGroup;

    public GameObject MainPauseGroup;

    public BizzaButton musicSwitchButton;
    public BizzaButton soundSwitchButton;
    public BizzaButton libSwitchButton;

    public Image musicOnIm;
    public Image musicOffIm;

    public Image soundOffIm;
    public Image soundOnIm;

    public Image LibOnIm;
    public Image LibOffIm;


    public TMP_Text LanguageText;

    public string[] languages = new string[8] { "","", "", "", "", "", "", ""};

    public TMP_Dropdown languageDropdown;

    private readonly Dictionary<string, string> displayNameMapping = new Dictionary<string, string>
    {
        { "en-US", "English" },
        { "pt-BR", "Português" },
        { "id-ID", "Bahasa Indonesia" },
        { "ru-RU", "Русский" },
        { "ja-JP", "日本語" },
        { "ko-KR", "한국어" },
        { "es-ES", "Español" },
        { "tr-TR", "Türkçe" },
        { "vi-VN", "Tiếng Việt" }
    };

    List<string> displayName = new List<string>() { "English" , "Português" , "Bahasa Indonesia", "Русский" , "日本語" , "한국어" , "Español" , "Türkçe" , "Tiếng Việt" };
    
    private bool musicIsOn
    {
        get => SaveDataUtils.SettingData.enableMusic;
        set => SaveDataUtils.SettingData.enableMusic = value;
    }

    private bool soundIsOn
    {
        get => SaveDataUtils.SettingData.enableSound;
        set => SaveDataUtils.SettingData.enableSound = value;
    }

    private bool libIsOn
    {
        get => SaveDataUtils.SettingData.enableVibrate;
        set => SaveDataUtils.SettingData.enableVibrate = value;
    }

    public float changeTime = 0.5f;

    private void Awake()
    {
        CloseButton.onClick.AddListener((() => {
            World.Current.Pause(this);
            this.CloseSelf();
        }));
        ContinueButton.onClick.AddListener(() =>
        {
            this.CloseSelf();
            World.Current.Resume(this);
        });
        BackButton.onClick.AddListener(RestartBtnClick);
        musicSwitchButton.onClick.AddListener(SwitchMusic);
        soundSwitchButton.onClick.AddListener(SwitchSound);
        libSwitchButton.onClick.AddListener(SwitchLib);

        MainBackButton.onClick.AddListener(() => CloseSelf());
    }

    protected override void OnOpen()
    {
        Init();
    }

    protected override void OnClose()
    {
        BizzaEventSystem.Emit(EventDefine.Frame.PausePanelClose);
    }

    public void TryBackMain()
    {
        CloseSelf();
    }

    public void RestartBtnClick()
    {
        CloseSelf();
        World.Current.Resume(this);

        FlowModule.LoadGameLevel();
    }

    public void SwitchMusic()
    {
        musicIsOn = !musicIsOn;

        SoundManager.Instance.MuteBGM(!musicIsOn);
        Refresh();
    }

    public void SwitchSound()
    {
        soundIsOn = !soundIsOn;
        SoundManager.Instance.MuteSFX(!soundIsOn);
        Refresh();
    }

    public void SwitchLib()
    {
        libIsOn = !libIsOn;

        Refresh();
    }

    public void Init()
    {
        if (World.Current.CurGameMode.GameModeType == E_GameModeType.MainMenu)
        {
            GamePauseGroup.SetActive(false);
            MainPauseGroup.SetActive(true);
        }
        else
        {
            GamePauseGroup.SetActive(true);
            MainPauseGroup.SetActive(false);
        }

        Refresh();
    }

    public void Refresh()
    {
        musicOnIm.gameObject.SetActive(musicIsOn);
        musicOffIm.gameObject.SetActive(!musicIsOn);
        soundOnIm.gameObject.SetActive(soundIsOn);
        soundOffIm.gameObject.SetActive(!soundIsOn);
        LibOnIm.gameObject.SetActive(libIsOn);
        LibOffIm.gameObject.SetActive(!libIsOn);
    }


    public void InitDropDown()
    {
        var options = new List<TMP_Dropdown.OptionData>();
        foreach (var item in displayName)
        {
            options.Add(new TMP_Dropdown.OptionData(item));
        }
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(options);
        languageDropdown.onValueChanged.AddListener(ChangeLanguage);
    }


    public void ChangeLanguage(int index)
    {
        switch (index)
        {
            case 0:LanguageUtils.SelectedLanguage = "en-US"; break;
            case 1:LanguageUtils.SelectedLanguage = "pt-BR"; break;
            case 2:LanguageUtils.SelectedLanguage = "id-ID"; break;
            case 3:LanguageUtils.SelectedLanguage = "ru-RU"; break;
            case 4:LanguageUtils.SelectedLanguage = "ja-JP"; break;
            case 5:LanguageUtils.SelectedLanguage = "ko-KR"; break;
            case 6:LanguageUtils.SelectedLanguage = "es-ES"; break;
            case 7:LanguageUtils.SelectedLanguage = "tr-TR"; break;
            case 8:LanguageUtils.SelectedLanguage = "vi-VN"; break;
        }

    }

    private void OnEnable()
    {
        InitDropDown();
        int res = 0;
        switch (LanguageUtils.SelectedLanguage)
        {
            case "en-US":
                res = 0;
                break;
            case "pt-BR":
                res = 1;
                break;
            case "id-ID":
                res = 2;
                break;
            case "ru-RU":
                res = 3;
                break;
            case "ja-JP":
                res = 4;
                break;
            case "ko-KR":
                res = 5;
                break;
            case "es-ES":
                res = 6;
                break;
            case "tr-TR":
                res = 7;
                break;
            case "vi-VN":
                res = 8;
                break;
        }
        languageDropdown.SetValueWithoutNotify(res);
        languageDropdown.RefreshShownValue();
    }
    
    
}
