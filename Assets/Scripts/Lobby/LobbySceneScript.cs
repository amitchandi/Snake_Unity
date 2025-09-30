using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using UnityEngine.SceneManagement;
using System;

public class LobbySceneScript : MonoBehaviour
{
    public GameObject userCard;
    public GameObject usersContainter;
    public TMP_InputField messageField;
    public Button sendBtn;
    public TMP_Text chatLog;
    public Button startBtn;

    public AudioSource menuTheme;

    GameObject client;
    GameObject settings;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(LoadServerCommunication());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnSetReady(DataModel message)
    {
        StartCoroutine(SetReady(message));
    }

    void OnChatMessage(DataModel message)
    {
        chatLog.text += "\n" + message.data["args"]["username"].ToString() + ": " + message.data["args"]["message"].ToString();
    }

    void OnStartGame()
    {
        StartCoroutine(LoadGame());
    }

    void OnJoinRoom(DataModel message)
    {
        var user = message.data["user"].ToObject<User>();
        if (user.Id != client.GetComponent<ServerCommunication>().GetUser().Id)
        {
            var card = Instantiate(userCard, usersContainter.transform);
            var cardScript = card.GetComponent<UserCard>();
            cardScript.SetUser(user);
        }
        StartCoroutine(CanStart());
    }

    void OnLeaveRoom(DataModel message)
    {
        string userId = message.data["userId"].ToString();
        if (userId != client.GetComponent<ServerCommunication>().GetUser().Id)
        {
            Destroy(GameObject.Find("UserCard:" + userId));
            string username = message.data["username"].ToString();
            chatLog.text += "\n" + username + " has left.";
        }
        else
        {
            StartCoroutine(LoadMain());
        }
    }

    void OnDeleteRoom()
    {
        client.GetComponent<ServerCommunication>().LeaveRoom();
    }

    void RemoveListeners()
    {
        ServerCommunication sc = client.GetComponent<ServerCommunication>();

        sc.Messaging.OnSetReady -= OnSetReady;
        sc.Messaging.OnChatMessage -= OnChatMessage;
        sc.Messaging.OnStartGame -= OnStartGame;
        sc.Messaging.OnJoinRoom -= OnJoinRoom;
        sc.Messaging.OnLeaveRoom -= OnLeaveRoom;
        sc.Messaging.OnDeleteRoom -= OnDeleteRoom;

    }

    IEnumerator LoadServerCommunication()
    {
        client = GameObject.Find("Client");
        settings = GameObject.Find("Settings");
        ServerCommunication sc = client.GetComponent<ServerCommunication>();
        yield return null;
        //yield return new WaitWhile(() => sc.isLoading);
        messageField.onEndEdit.AddListener(SendChatMessage);
        sendBtn.onClick.AddListener(delegate
        {
            SendChatMessage(messageField.text);
        });

        startBtn.onClick.AddListener(delegate
        {
            sc.SendStartGame();
        });

        sc.GetRoom().users.ForEach(user =>
        {
            var card = Instantiate(userCard, usersContainter.transform);
            var cardScript = card.GetComponent<UserCard>();
            cardScript.SetUser(user);
        });

        sc.Messaging.OnSetReady += OnSetReady;
        sc.Messaging.OnChatMessage += OnChatMessage;
        sc.Messaging.OnStartGame += OnStartGame;
        sc.Messaging.OnJoinRoom += OnJoinRoom;
        sc.Messaging.OnLeaveRoom += OnLeaveRoom;
        sc.Messaging.OnDeleteRoom += OnDeleteRoom;

        LoadSettings();
        menuTheme.Play();
    }

    public void SendChatMessage(string message)
    {
        if (message.Length > 0)
        {
            client.GetComponent<ServerCommunication>().SendChatMessage(message);
            messageField.SetTextWithoutNotify("");
        }
    }

    public void SetReadyStatus()
    {
        client.GetComponent<ServerCommunication>().SendReadyChange(false);
    }

    public void LeaveRoom()
    {
        var sc = client.GetComponent<ServerCommunication>();
        sc.LeaveRoom();
    }

    public void LoadSettings()
    {
        float BGM_Volume = settings.GetComponent<Settings>().savedSettings.BGM_Volume;
        menuTheme.volume = BGM_Volume * 0.5f;
    }

    IEnumerator LoadGame()
    {
        RemoveListeners();

        yield return new WaitForEndOfFrame();
        SceneManager.LoadSceneAsync("Game", LoadSceneMode.Single);
    }

    IEnumerator LoadMain()
    {
        RemoveListeners();

        yield return new WaitForEndOfFrame();
        SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
    }

    IEnumerator SetReady(DataModel data)
    {
        yield return null;
        var json = JObject.FromObject(data);
        GameObject.Find("UserCard:" + json["data"]["args"]["userId"]).GetComponent<UserCard>()
            .ChangeReadyStatus(json["data"]["args"]["isReady"].ToObject<bool>());
        StartCoroutine(CanStart());
    }

    IEnumerator CanStart()
    {
        yield return null;
        bool canStart = true;
        var userCards = GameObject.FindGameObjectsWithTag("UserCard");
        foreach (var card in userCards)
        {
            if (!card.GetComponent<UserCard>().IsReady())
                canStart = false;
        }
        startBtn.interactable = canStart;
    }
}
