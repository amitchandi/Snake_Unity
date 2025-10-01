using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
    private string Host => useLocalhost ? "localhost" : hostIP;
    // Final server address
    private string server;

    // WebSocket Client
    private WsClient client;

    // HTTP Client
    private HTTPClient httpClient;

    // JWT
    string JWT { get; set; }

    User user = new();
    public GameLobby Lobby { get; set; } = new GameLobby();
    //

    // Class with messages for "Lobby"
    public GameMessaging Messaging { private set; get; }

    #region MonoBehaviour
    /// <summary>
    /// Unity method called on initialization
    /// </summary>
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this);

            server = Host + ":" + port;
            httpClient = new HTTPClient("http://" + server);

            // // Messaging
            // Messaging = new GameMessaging(this);
        }
        else if (instance != this)
        {
            DestroySelf();
        }
    }

    private async void OnDestroy()
    {
        Debug.Log("ServerCommunication Destroy.");
        await CloseConnectionToServer();
    }

    //private async void OnApplicationQuit()
    //{
    //    await CloseConnectionToServer();
    //}
    #endregion

    private void DestroySelf()
    {
        if (Application.isPlaying)
            Destroy(this);
        else
            DestroyImmediate(this);
    }

    /// <summary>
    /// Unity method called every frame
    /// </summary>
    private void Update()
    {
        if (client != null && !client.ReceiveQueue.IsEmpty)
        {
            // Check if server send new messages
            var cqueue = client.ReceiveQueue;
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

        try
        {
            // Deserializing message from the server
            var message = JsonConvert.DeserializeObject<DataModel>(msg);

            // Picking correct method for message handling
            switch (message.@event)
            {
                case GameMessaging.REGISTER:
                    Messaging.OnConnectedToServer?.Invoke();
                    break;
                case GameMessaging.ECHO:
                    Messaging.OnEchoMessage?.Invoke(JsonUtility.FromJson<EchoMessageModel>(message.data.ToString()));
                    break;

                //case "getRoom":
                //    room = message.data["room"].ToObject<Room>();
                //    Messaging.OnGetLobby?.Invoke(message);
                //    break;
                //case "joinRoom":
                //    room = message.data["room"].ToObject<Room>();
                //    Debug.Log("Joined:" + room);
                //    Messaging.OnJoinLobby?.Invoke(message);
                //    break;
                //case "leaveRoom":
                //    string userId = message.data["userId"].ToString();
                //    if (userId == user.Id)
                //    {
                //        room = null;
                //        user.IsReady = false;
                //    }
                //    Messaging.OnLeaveRoom?.Invoke(message);
                //    break;
            
            
                case "eatPellet":
                    Messaging.OnEatPellet?.Invoke(message);
                    break;
                case "die":
                    Messaging.OnDie?.Invoke(message);
                    break;
                case "chatMessage":
                    Messaging.OnChatMessage?.Invoke(message);
                    break;
                case "winner":
                    Messaging.OnWin?.Invoke(message);
                    break;
                case "reset":
                    Messaging.OnReset?.Invoke(message);
                    break;
                case "zoom":
                    Messaging.OnZoom?.Invoke(message);
                    break;
                case "slow":
                    Messaging.OnSlow?.Invoke(message);
                    break;
                case "invincible":
                    Messaging.OnInvincible?.Invoke(message);
                    break;
                case "getGameState":
                    Messaging.OnGetGameState?.Invoke(message);
                    break;

                
                case GameMessaging.START_GAME:
                    Messaging.OnStartGame?.Invoke();
                    break;
                case GameMessaging.LOBBY_PLAYERS:
                    Messaging.OnLobbyPlayers?.Invoke(message);
                    break;
                case GameMessaging.LOBBY_JOINED:
                    Messaging.OnLobbyJoined?.Invoke(message);
                    break;

                default:
                    Debug.LogError("Unknown type of method: " + message.@event);
                    break;
            }
        }
        catch (Exception)
        {
            Debug.LogError("Invalid JSON");
        }
    }

    /// <summary>
    /// Call this method to connect to the server
    /// </summary>
    public async Task ConnectToServer()
    {
        client = new WsClient("ws://" + server, JWT);
        await client.ConnectAsync();
        Messaging = new GameMessaging(this);
    }

    /// <summary>
    /// Call this method to connect to the server
    /// </summary>
    public async Task CloseConnectionToServer()
    {
        if (client != null)
            await client.CloseAsync();
        httpClient?.Dispose();
    }

    /// <summary>
    /// Method which sends data through websocket
    /// </summary>
    /// <param name="event_name">Event Username</param>
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
            lobbyId = Lobby.id,
            args = new
            {
                userId = user.Id
            }
        }));
    }

    public string GetName()
    {
        return user == null ? "" : user.Username;
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
            lobbyId = Lobby.id,
            userId = user.Id
        }));
    }

    public void SendChatMessage(string message)
    {
        Send("chatMessage", JObject.FromObject(new
        {
            lobbyId = Lobby.id,
            args = new
            {
                username = user.Username,
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
                lobbyId = Lobby.id,
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
                lobbyId = Lobby.id,
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
            lobbyId = Lobby.id,
            args = new
            {

            }
        }));
    }

    public void EatPellet(int SnakeSize)
    {
        Send("eatPellet", JObject.FromObject(new
        {
            lobbyId = Lobby.id,
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
            lobbyId = Lobby.id,
            args = new
            {
                userId = user.Id,
                username = user.Username
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

    public User GetUser()
    {
        return user;
    }

    //public async void RetrieveUser()
    //{
    //    user = await GetUserRequest(SystemInfo.deviceUniqueIdentifier);
    //    if (user == null)
    //    {
    //        await CreateUserRequest(SystemInfo.deviceUniqueIdentifier, "name");
    //        RetrieveUser();
    //    }
    //}

    public async void UpdateUsername(string username)
    {
        bool updated = await UpdateUsernameRequest(user.Email, username);
        if (updated)
            user.Username = username;
    }

    public async Task<bool> Login(string email, string password)
    {
        var (valid, content) = await httpClient.Login(email, password);
        if (valid)
        {
            var json = JObject.Parse(content);
            Debug.Log(json["token"]);
            JWT = (string)json["token"];
            user.Username = (string)json["username"];
            user.Email = (string)json["email"];
            user.Id = (string)json["userId"];
            user.Wins = (int)json["wins"];
            user.GamesPlayed = (int)json["gamesPlayed"];
        }
        return true;
    }

    public async Task<bool> Register(string email, string username, string password)
    {
        var res = await httpClient.Register(email, username, password);
        if (res)
        {
            return await Login(email, password);
        }
        return false;
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
    
    private async Task<User> GetUserRequest(string deviceId)
    {
        var user = await httpClient.GetUser(deviceId);
        if (user == null)
            return null;
        else
            return JsonConvert.DeserializeObject<User>(user);
    }

    private async Task<bool> UpdateUsernameRequest(string name, string newUsername)
    {
        return await httpClient.UpdateUserName(name, newUsername);
    }

    //private async Task<bool> LoginRequest(string email, string password)
    //{
    //    return await httpClient;
    //}

    #endregion
}
