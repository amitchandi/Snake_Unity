using Newtonsoft.Json;
using System.IO;
using UnityEngine;

public class Settings : MonoBehaviour
{
    public SavedSettings savedSettings;
    
    private static Settings instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);

            LoadFromFile();
            if (Application.platform != RuntimePlatform.Android && Application.platform != RuntimePlatform.IPhonePlayer)
                Screen.SetResolution(savedSettings.Screen_Width, savedSettings.Screen_Height, savedSettings.Is_Full_Screen, savedSettings.Screen_Refresh_Rate);
        }
        else if (instance != this)
        {
            DestroySelf();
        }
    }

    private void DestroySelf()
    {
        if (Application.isPlaying)
            Destroy(this);
        else
            DestroyImmediate(this);
    }

    public void ChangeBGMVolume(float volume)
    {
        savedSettings.BGM_Volume = volume;
        SaveToFile();
    }

    public void ChangeSFXVolume(float volume)
    {
        savedSettings.SFX_Volume = volume;
        SaveToFile();
    }

    public void ChangeScreenResolution(Resolution resolution)
    {
        savedSettings.Screen_Width = resolution.width;
        savedSettings.Screen_Height = resolution.height;
        savedSettings.Screen_Refresh_Rate = resolution.refreshRate;
        SaveToFile();
    }

    public void ChangeFullscreen(bool isFullscreen, FullScreenMode fullScreenMode)
    {
        savedSettings.Is_Full_Screen = isFullscreen;
        savedSettings.Full_Screen_Mode = fullScreenMode;
        SaveToFile();
    }

    public void SaveToFile()
    {
        string path;
        switch (Application.platform)
        {
            case RuntimePlatform.IPhonePlayer:
                path = Application.persistentDataPath;
                break;
            case RuntimePlatform.Android:
                path = Application.temporaryCachePath;
                break;
            default:
                path = Application.dataPath;
                break;
        }
        File.WriteAllText(path + "/settings.json", JsonConvert.SerializeObject(savedSettings));
    }

    public void LoadFromFile()
    {
        if (File.Exists(Application.dataPath + "/settings.json"))
        {
            string jsonSettings = File.ReadAllText(Application.dataPath + "/settings.json");
            SavedSettings settings = JsonConvert.DeserializeObject<SavedSettings>(jsonSettings);
            savedSettings = settings;
        }
        else
        {
            savedSettings = new SavedSettings
            {
                BGM_Volume = 0.5f,
                SFX_Volume = 0.5f,
                Screen_Width = 1920,
                Screen_Height = 1080,
                Screen_Refresh_Rate = 59,
                Is_Full_Screen = false,
                Full_Screen_Mode = FullScreenMode.Windowed
            };
            SaveToFile();
        }
    }
}

public class SavedSettings
{
    public float BGM_Volume { get; set; }
    public float SFX_Volume { get; set; }
    public int Screen_Width { get; set; }
    public int Screen_Height { get; set; }
    public int Screen_Refresh_Rate { get; set; }
    public bool Is_Full_Screen { get; set; }
    public FullScreenMode Full_Screen_Mode { get; set; }
}

