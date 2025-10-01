using Assets.Scripts;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TMPro.TMP_Dropdown;

public class Menu : MonoBehaviour
{
    public TMP_InputField usernameField;

    public TMP_InputField loginEmailField;
    public TMP_InputField loginPasswordField;

    public TMP_InputField registerEmailField;
    public TMP_InputField registerUsernameField;
    public TMP_InputField registerPasswordField;
    public TMP_InputField registerPasswordConfirmField;

    public GameObject resolutionRow;
    
    public Slider BGM_Slider;
    public Slider SFX_Slider;

    public TMP_Dropdown resDropdown;
    public TMP_Dropdown fullsDropdown;

    public Toggle isFullscreen;

    public AudioMixer MainMixer;
    
    public AudioSource menuClick;
    public AudioSource menuTheme;
    public AudioSource testSFX;

    public GameObject MainMenu;
    public GameObject FindGameMenu;
    public GameObject SettingsMenu;
    //public GameObject LobbiesMenu;
    public GameObject LoginMenu;
    public GameObject RegisterMenu;

    GameObject ClientObject;
    ServerCommunication ServerCommunication;

    GameObject SettingsObject;
    Settings SettingsScript;

    // Start is called before the first frame update
    private void Start()
    {
        StartCoroutine(LoadServerCommunication());
    }

    private void Awake()
    {
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer)
            Destroy(resolutionRow);
    }

    // Update is called once per frame
    private void Update()
    {
        
    }

    private void OnDestroy()
    {
        ResetGameMessaging();
    }

    IEnumerator LoadServerCommunication()
    {
        ClientObject = GameObject.Find("Client");
        ServerCommunication = ClientObject.GetComponent<ServerCommunication>();

        SettingsObject = GameObject.Find("Settings");
        SettingsScript = SettingsObject.GetComponent<Settings>();

        usernameField.onValueChanged.AddListener(UpdateName);
        
        //TODO: Implement auto login

        LoadSettings();

        BGM_Slider.onValueChanged.AddListener(volume =>
        {
            SettingsScript.ChangeBGMVolume(volume);
            MainMixer.SetFloat("BGMVolume", Mathf.Log10(volume) * 20);
        });

        SFX_Slider.onValueChanged.AddListener(volume =>
        {
            SettingsScript.ChangeSFXVolume(volume);
            MainMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
            testSFX.Play();
        });

        resDropdown.options.AddRange(Array.ConvertAll(Screen.resolutions, res => new OptionData(res.ToString())));

        resDropdown.onValueChanged.AddListener(index =>
        {
            Resolution resolution = Screen.resolutions[index];
            SettingsScript.ChangeScreenResolution(resolution);
            if (isFullscreen.isOn)
                Screen.SetResolution(resolution.width, resolution.height, (FullScreenMode)fullsDropdown.value, resolution.refreshRateRatio);
            else
                Screen.SetResolution(resolution.width, resolution.height, false);
        });

        fullsDropdown.options.AddRange(Array.ConvertAll(Enum.GetNames(typeof(FullScreenMode)), mode => new OptionData(mode)));

        fullsDropdown.onValueChanged.AddListener(index =>
        {
            SettingsScript.ChangeFullscreen(isFullscreen.isOn, (FullScreenMode)index);
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, (FullScreenMode)index, Screen.currentResolution.refreshRateRatio);
        });

        isFullscreen.onValueChanged.AddListener(isFullscreen =>
        {
            SettingsScript.ChangeFullscreen(isFullscreen, (FullScreenMode)fullsDropdown.value);
            if (isFullscreen)
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, (FullScreenMode)fullsDropdown.value, Screen.currentResolution.refreshRateRatio);
            else
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, false);
            fullsDropdown.interactable = isFullscreen;
        });

        menuTheme.Play();
        yield return null;
    }

    private void UpdateName(string name)
    {
        ServerCommunication.UpdateUsername(name);
    }

    public void LoadSettings()
    {
        float BGM_Volume = SettingsScript.savedSettings.BGM_Volume;
        float SFX_Volume = SettingsScript.savedSettings.SFX_Volume;
        BGM_Slider.value = BGM_Volume;
        SFX_Slider.value = SFX_Volume;

        MainMixer.SetFloat("BGMVolume", Mathf.Log10(BGM_Volume) * 20);
        MainMixer.SetFloat("SFXVolume", Mathf.Log10(SFX_Volume) * 20);

        if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
        {
            for (int i = 0; i < Screen.resolutions.Length; i++)
            {
                Resolution res = Screen.resolutions[i];
                if (SettingsScript.savedSettings.Screen_Width == res.width && SettingsScript.savedSettings.Screen_Height == res.height && SettingsScript.savedSettings.RefreshRate.Equals(res.refreshRateRatio))
                    resDropdown.value = i;
            }

            isFullscreen.isOn = SettingsScript.savedSettings.Is_Full_Screen;
            fullsDropdown.interactable = SettingsScript.savedSettings.Is_Full_Screen;
        }
    }

    #region Menu Button Functions
    public void ExitGame()
    {
        Application.Quit(0);
    }

    public async void Login()
    {
        await ServerCommunication.Login(loginEmailField.text, loginPasswordField.text);
        usernameField.SetTextWithoutNotify(ServerCommunication.GetUser().Username);

        LoginMenu.SetActive(false);
        MainMenu.SetActive(true);
    }

    public async void Register()
    {
        if (registerPasswordField.text != registerPasswordConfirmField.text)
        {
            Debug.LogError("Passwords dont match");
            //TODO: implement Error message in UI about mismatch passwords

            return;
        }
        await ServerCommunication.Register(registerEmailField.text, registerUsernameField.text, registerPasswordField.text);
        usernameField.SetTextWithoutNotify(ServerCommunication.GetUser().Username);

        RegisterMenu.SetActive(false);
        MainMenu.SetActive(true);
    }

    public void ClickMenu()
    {
        menuClick.Play();
    }

    public async void FindGameClicked()
    {
        await ServerCommunication.ConnectToServer();
        InitGameMessaging();
    }
    #endregion

    #region GameMessaging
    private void InitGameMessaging()
    {
        Debug.Log("InitGameMessaging");
        Debug.Log(ServerCommunication.Messaging);
        ServerCommunication.Messaging.OnStartGame += OnStartGame;
        ServerCommunication.Messaging.OnLobbyJoined += OnLobbyJoined;
        ServerCommunication.Messaging.OnLobbyDisconnected += OnLobbyDisconnected;
        ServerCommunication.Messaging.OnLobbyPlayers += OnLobbyPlayers;
    }

    private void ResetGameMessaging()
    {
        if (ServerCommunication == null || ServerCommunication.Messaging == null) return;

        ServerCommunication.Messaging.OnStartGame -= OnStartGame;
        ServerCommunication.Messaging.OnLobbyJoined -= OnLobbyJoined;
        ServerCommunication.Messaging.OnLobbyDisconnected -= OnLobbyDisconnected;
        ServerCommunication.Messaging.OnLobbyPlayers -= OnLobbyPlayers;
    }

    private void OnStartGame()
    {
        Debug.Log("Start Game");
        SceneManager.LoadSceneAsync(Scenes.GAME, LoadSceneMode.Single);
        MyLobby.Lobby = ServerCommunication.Lobby;
    }

    private void OnLobbyJoined(DataModel message)
    {
        Debug.Log("Lobby Joined");
        Debug.Log(message);
        ServerCommunication.Lobby = message.data["lobby"].ToObject<GameLobby>();
    }

    private void OnLobbyDisconnected(DataModel message)
    {
        Debug.Log("Lobby Disconnected");
        Debug.Log(message);
    }

    private void OnLobbyPlayers(DataModel message)
    {
        Debug.Log("Lobby Players");
        Debug.Log(message);
        foreach (var item in message.data["players"].Values<string>().ToArray())
        {
            Debug.Log(item);
        }
        ServerCommunication.Lobby.players = message.data["players"].Values<string>().ToArray();
    }
    #endregion
}
