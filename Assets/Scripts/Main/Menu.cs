using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TMPro.TMP_Dropdown;

public class Menu : MonoBehaviour
{
    public TMP_InputField nameField;
    public TMP_InputField loginEmailField;
    public TMP_InputField loginPasswordField;

    public GameObject Lobby;
    public GameObject resolutionRow;
    
    public Slider BGM_Slider;
    public Slider SFX_Slider;

    public TMP_Dropdown resDropdown;
    public TMP_Dropdown fullsDropdown;

    public Toggle isFullscreen;

    public AudioSource menuClick;
    public AudioSource menuTheme;
    public AudioSource wallDropSound;

    GameObject client;
    GameObject settings;

    public void ClickMenu()
    {
        menuClick.Play();
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LoadServerCommunication());
    }

    private void Awake()
    {
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            Destroy(resolutionRow);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator LoadServerCommunication()
    {
        client = GameObject.Find("Client");
        settings = GameObject.Find("Settings");
        ServerCommunication sc = client.GetComponent<ServerCommunication>();
        yield return new WaitWhile(() => sc.isLoading);
        nameField.onValueChanged.AddListener(UpdateName);
        string name = sc.GetName();
        nameField.SetTextWithoutNotify(name);

        LoadSettings();

        BGM_Slider.onValueChanged.AddListener((float volume) =>
        {
            settings.GetComponent<Settings>().ChangeBGMVolume(volume);
            menuTheme.volume = volume * 0.5f;
        });

        SFX_Slider.onValueChanged.AddListener((float volume) =>
        {
            settings.GetComponent<Settings>().ChangeSFXVolume(volume);
            wallDropSound.volume = volume * 0.5f;
            wallDropSound.time = 0.5f;
            wallDropSound.Play();
        });

        resDropdown.options.AddRange(Array.ConvertAll(Screen.resolutions, res => new OptionData(res.ToString())));

        resDropdown.onValueChanged.AddListener((int index) =>
        {
            Resolution resolution = Screen.resolutions[index];
            settings.GetComponent<Settings>().ChangeScreenResolution(resolution);
            if (isFullscreen.isOn)
                Screen.SetResolution(resolution.width, resolution.height, (FullScreenMode)fullsDropdown.value, resolution.refreshRate);
            else
                Screen.SetResolution(resolution.width, resolution.height, isFullscreen.isOn, resolution.refreshRate);
        });

        fullsDropdown.options.AddRange(Array.ConvertAll(Enum.GetNames(typeof(FullScreenMode)), mode => new OptionData(mode)));

        fullsDropdown.onValueChanged.AddListener((int index) =>
        {
            settings.GetComponent<Settings>().ChangeFullscreen(isFullscreen.isOn, (FullScreenMode)index);
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, (FullScreenMode)index, Screen.currentResolution.refreshRate);
        });

        isFullscreen.onValueChanged.AddListener((bool isFullscreen) =>
        {
            settings.GetComponent<Settings>().ChangeFullscreen(isFullscreen, (FullScreenMode)fullsDropdown.value);
            if (isFullscreen)
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, (FullScreenMode)fullsDropdown.value, Screen.currentResolution.refreshRate);
            else
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, isFullscreen, Screen.currentResolution.refreshRate);
            fullsDropdown.interactable = isFullscreen;
        });

        menuTheme.Play();
    }

    IEnumerator LoadLobby()
    {
        yield return new WaitWhile(() => client.GetComponent<ServerCommunication>().GetRoom() == null);
        SceneManager.LoadSceneAsync("Lobby", LoadSceneMode.Single);
    }

    void UpdateName(string name)
    {
        var sc = client.GetComponent<ServerCommunication>();
        sc.UpdateUsername(name);
    }

    public async void GetRooms()
    {
        var lobbiesPanel = GameObject.Find("LobbiesContent").transform;

        foreach (Transform child in lobbiesPanel)
            Destroy(child.gameObject);

        var rooms = await client.GetComponent<ServerCommunication>().GetRooms();
        foreach (var kv in rooms)
        {
            var x = Instantiate(Lobby, lobbiesPanel);
            x.GetComponent<Lobby>().SetRoom(kv.Value);
            Debug.Log(kv);
        }
    }

    public void StartLobby()
    {
        StartCoroutine(LoadLobby());
    }


    public void LoadSettings()
    {
        Settings settingsScript = settings.GetComponent<Settings>();

        float BGM_Volume = settingsScript.savedSettings.BGM_Volume;
        float SFX_Volume = settingsScript.savedSettings.SFX_Volume;
        BGM_Slider.value = BGM_Volume;
        SFX_Slider.value = SFX_Volume;
        menuTheme.volume = BGM_Volume * 0.5f;

        if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
        {
            for (int i = 0; i < Screen.resolutions.Length; i++)
            {
                Resolution res = Screen.resolutions[i];
                if (settingsScript.savedSettings.Screen_Width == res.width && settingsScript.savedSettings.Screen_Height == res.height && settingsScript.savedSettings.Screen_Refresh_Rate == res.refreshRate)
                    resDropdown.value = i;
            }

            isFullscreen.isOn = settingsScript.savedSettings.Is_Full_Screen;
            fullsDropdown.interactable = settingsScript.savedSettings.Is_Full_Screen;
        }
    }

    #region Menu Button Functions
    public void ExitGame()
    {
        Application.Quit(0);
    }

    public async void Login()
    {
        var sc = client.GetComponent<ServerCommunication>();
        await sc.Login(loginEmailField.text, loginPasswordField.text);
    }

    public void Register()
    {

    }
    #endregion
}
