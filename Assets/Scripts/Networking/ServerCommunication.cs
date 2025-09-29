using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Forefront class for the server communication.
/// </summary>
public class ServerCommunication : MonoBehaviour
{
    private static ServerCommunication instance;

    // Server IP address
    [SerializeField]
    private string hostIP;

    // Server port
    [SerializeField]
    private int port = 9001;

    // Flag to use localhost
    [SerializeField]
    private bool useLocalhost = true;

    // Address used in code
    private string host => useLocalhost ? "localhost" : hostIP;
    // Final server address
    private string server;

    // WebSocket Client
    private WsClient client;

    // HTTP Client
    private HTTPClient httpClient;

    // JWT
    string JWT;

    User user;
    Room room;

    public bool isLoading = true;
    //

    // Class with messages for "lobby"
    public LobbyMessaging Lobby { private set; get; }

    /// <summary>
    /// Unity method called on initialization
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);

            server = host + ":" + port;
             client = new WsClient("ws://" + server);
            httpClient = new HTTPClient("http://" + server);

            // // Messaging
            // Lobby = new LobbyMessaging(this);

            //ConnectToServer();
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

    IEnumerator Init()
    {
        //GetToken();
        //yield return new WaitWhile(() => JWT == null);
        //Debug.Log(JWT);

        RetrieveUser();
        yield return new WaitWhile(() => user == null);
        Debug.Log(user);

        isLoading = false;
    }

    private void OnApplicationQuit()
    {
        if (room != null)
        {
            LeaveRoom();
        }
        CloseConnectionToServer();
    }

    /// <summary>
    /// Unity method called every frame
    /// </summary>
    private void Update()
    {
        if (client != null && !client.receiveQueue.IsEmpty)
        {
            // Check if server send new messages
            var cqueue = client.receiveQueue;
            string msg;
            while (cqueue.TryPeek(out msg))
            {
                // Parse newly received messages
                cqueue.TryDequeue(out msg);
                HandleMessage(msg);
            }
        }
    }

    /// <summary>
    /// Method responsible for handling server messages
    /// </summary>
    /// <param name="msg">Message.</param>
    private void HandleMessage(string msg)
    {
        Debug.Log("Server: " + msg);

        // Deserializing message from the server
        var message = JsonConvert.DeserializeObject<DataModel>(msg);

        // Picking correct method for message handling
        switch (message.@event)
        {
            case LobbyMessaging.Register:
                Lobby.OnConnectedToServer?.Invoke();
                break;
            case LobbyMessaging.Echo:
                Lobby.OnEchoMessage?.Invoke(JsonUtility.FromJson<EchoMessageModel>(message.data.ToString()));
                break;
            case "getRoom":
                room = message.data["room"].ToObject<Room>();
                Lobby.OnGetRoom?.Invoke(message);
                break;
            case "joinRoom":
                room = message.data["room"].ToObject<Room>();
                Debug.Log("Joined:" + room);
                Lobby.OnJoinRoom?.Invoke(message);
                break;
            case "leaveRoom":
                string userId = message.data["userId"].ToString();
                if (userId == user.Id)
                {
                    room = null;
                    user.IsReady = false;
                }
                Lobby.OnLeaveRoom?.Invoke(message);
                break;
            case "deleteRoom":
                Lobby.OnDeleteRoom?.Invoke();
                break;
            case "setReadyStatus":
                room = message.data["room"].ToObject<Room>();
                RetrieveUser();
                Lobby.OnSetReady?.Invoke(message);
                break;
            case "eatPellet":
                Lobby.OnEatPellet?.Invoke(message);
                break;
            case "die":
                Lobby.OnDie?.Invoke(message);
                break;
            case "chatMessage":
                Lobby.OnChatMessage?.Invoke(message);
                break;
            case "startGame":
                Lobby.OnStartGame?.Invoke();
                break;
            case "winner":
                Lobby.OnWin?.Invoke(message);
                break;
            case "reset":
                Lobby.OnReset?.Invoke(message);
                break;
            case "zoom":
                Lobby.OnZoom?.Invoke(message);
                break;
            case "slow":
                Lobby.OnSlow?.Invoke(message);
                break;
            case "invincible":
                Lobby.OnInvincible?.Invoke(message);
                break;
            case "getGameState":
                Lobby.OnGetGameState?.Invoke(message);
                break;
            default:
                Debug.LogError("Unknown type of method: " + message.@event);
                break;
        }
    }

    /// <summary>
    /// Call this method to connect to the server
    /// </summary>
    public async void ConnectToServer()
    {
        await client.Connect();
        StartCoroutine(Init());
    }

    /// <summary>
    /// Call this method to connect to the server
    /// </summary>
    public async void CloseConnectionToServer()
    {
        if (client != null)
        {
            if (client.IsConnectionOpen())
                await client.Close();
            client.Dispose();
        }
        if (httpClient != null)
        {
            httpClient.Dispose();
        }
    }

    /// <summary>
    /// Method which sends data through websocket
    /// </summary>
    /// <param name="event_name">Event Name</param>
    /// /// <param name="event_data">Event Data (parameters)</param>
    public void Send(string event_name, JObject event_data)
    {
        JObject o = JObject.FromObject(new
        {
            @event = event_name,
            @data = event_data
        });

        try
        {
            Task.Run(() =>
            {
                client.Send(o.ToString());
            }
            );
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    #region [Public Methods]

    public void SendSpecial(Special special)
    {
        Send(Enum.GetName(typeof(Special), special), JObject.FromObject(new
        {
            roomId = room.id,
            args = new
            {
                userId = user.Id
            }
        }));
    }

    public string GetName()
    {
        return user.Name;
    }

    public async Task<Dictionary<string, Room>> GetRooms()
    {
        return await GetRoomsRequest();
    }

    public void JoinRoom(string roomId)
    {
        Send("joinRoom", JObject.FromObject(new
        {
            roomId,
            userId = user.Id
        }));
    }

    public void LeaveRoom()
    {
        Send("leaveRoom", JObject.FromObject(new
        {
            roomId = room.id,
            userId = user.Id
        }));
    }

    public void SendChatMessage(string message)
    {
        Send("chatMessage", JObject.FromObject(new
        {
            roomId = room.id,
            args = new
            {
                username = user.Name,
                message
            }
        }));
    }

    public void SendReadyChange(bool setFalse)
    {
        if (setFalse)
        {
            Send("setReadyStatus", JObject.FromObject(new
            {
                userId = user.Id,
                roomId = room.id,
                args = new
                {
                    userId = user.Id,
                    isReady = false
                }
            }));
        }
        else
        {
            Send("setReadyStatus", JObject.FromObject(new
            {
                userId = user.Id,
                roomId = room.id,
                args = new
                {
                    userId = user.Id,
                    isReady = !user.IsReady
                }
            }));
        }
    }

    public void SendStartGame()
    {
        Send("startGame", JObject.FromObject(new
        {
            roomId = room.id,
            args = new
            {

            }
        }));
    }

    public void EatPellet(int SnakeSize)
    {
        Send("eatPellet", JObject.FromObject(new
        {
            roomId = room.id,
            args = new
            {
                userId = user.Id,
                magnitude = SnakeSize
            }
        }));
    }

    public void SendSetGameState(GameState gameState)
    {
        Send("setGameState", JObject.FromObject(new
        {
            args = new
            {
                userId = user.Id,
                gameState
            }
        }));
    }

    public void SendGetGameState(string userId)
    {
        Send("getGameState", JObject.FromObject(new
        {
            args = new
            {
                userId,
            }
        }));
    }

    public void Die()
    {
        Send("die", JObject.FromObject(new
        {
            userId = user.Id,
            roomId = room.id,
            args = new
            {
                userId = user.Id,
                username = user.Name
            }
        }));
    }

    public void Ping()
    {
        Send("ping", JObject.FromObject(new
        {
            args = new
            {
                msg = "asd"
            }
        }));
    }

    public async Task<Room> CreateRoom(int wallsToStart)
    {
        room = await CreateRoomRequest(user.Name + "'s Room", true, user.Id, wallsToStart);
        return room;
    }

    public void GetRoom(string roomId)
    {
        Send("getRoom", JObject.FromObject(new
        {
            roomId,
        }));
    }

    public Room GetRoom()
    {
        return room;
    }

    public User GetUser()
    {
        return user;
    }

    public async void RetrieveUser()
    {
        user = await GetUserRequest(SystemInfo.deviceUniqueIdentifier);
        if (user == null)
        {
            await CreateUserRequest(SystemInfo.deviceUniqueIdentifier, "name");
            RetrieveUser();
        }
    }

    public async void UpdateUsername(string username)
    {
        bool updated = await UpdateUsernameRequest(username, SystemInfo.deviceUniqueIdentifier);
        if (updated)
            user.Name = username;
    }

    public async Task<bool> Login(string email, string password)
    {
        var res = await httpClient.Login(email, password);
        if (res.valid)
        {
            var json = JObject.Parse(res.content);
            JWT = (string)json["token"];
        }
        return true;
    }

    public void AddWin()
    {
        Send("addWin", JObject.FromObject(new
        {
            userId = user.Id
        }));
    }
    #endregion

    #region [HTTP]

    private async Task<string> GetTokenRequest()
    {
        return await httpClient.GetToken();
    }

    private async Task<User> GetUserRequest(string deviceId)
    {
        var user = await httpClient.GetUser(deviceId);
        if (user == null)
            return null;
        else
            return JsonConvert.DeserializeObject<User>(user);
    }

    private async Task<Dictionary<string, Room>> GetRoomsRequest()
    {
        var x = await httpClient.GetRooms();
        return JsonConvert.DeserializeObject<Dictionary<string, Room>>(x);
    }

    private async Task<string> CreateUserRequest(string deviceId, string name)
    {
        return await httpClient.CreateUser(deviceId, name);
    }

    private async Task<Room> CreateRoomRequest(string roomName, bool isGameRoom, string ownerId, int wallsToStart)
    {
        return JsonConvert.DeserializeObject<Room>(await httpClient.CreateRoom(roomName, isGameRoom, ownerId, wallsToStart));
    }

    private async Task<bool> UpdateUsernameRequest(string name, string deviceId)
    {
        return await httpClient.UpdateUserName(name, deviceId);
    }

    //private async Task<bool> LoginRequest(string email, string password)
    //{
    //    return await httpClient;
    //}

    #endregion
}
