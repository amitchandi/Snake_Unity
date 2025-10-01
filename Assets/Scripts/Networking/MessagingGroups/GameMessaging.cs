using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Class holding game messages.
/// </summary>
public class GameMessaging : BaseMessaging
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:GameMessaging"/> class.
    /// </summary>
    /// <param name="client">Client.</param>
    public GameMessaging(ServerCommunication client) : base(client) { }

    // REGISTER messages
    public const string REGISTER = "register";
    public UnityAction OnConnectedToServer;

    // ECHO messages
    public const string ECHO = "echo";
    public UnityAction<EchoMessageModel> OnEchoMessage;

    public UnityAction<DataModel> OnGetGameState;
    // On Get Lobby
    public UnityAction<DataModel> OnGetLobby;

    // On Chat Message
    public UnityAction<DataModel> OnChatMessage;


    // On Eat Pellet
    public UnityAction<DataModel> OnEatPellet;
    // On Die
    public UnityAction<DataModel> OnDie;
    // On Win
    public UnityAction<DataModel> OnWin;
    // On Reset
    public UnityAction<DataModel> OnReset;
    // On Zoom
    public UnityAction<DataModel> OnZoom;
    // On Slow
    public UnityAction<DataModel> OnSlow;
    // On Invincible
    public UnityAction<DataModel> OnInvincible;


    public const string START_GAME = "startGame";
    public UnityAction OnStartGame;

    public const string LOBBY_PLAYERS = "lobbyPlayers";
    public UnityAction<DataModel> OnLobbyPlayers;

    public const string LOBBY_JOINED = "lobbyJoined";
    public UnityAction<DataModel> OnLobbyJoined;

    public const string LOBBY_DISCONNECTED = "lobbyDisconnected";
    public UnityAction<DataModel> OnLobbyDisconnected;

    /// <summary>
    /// Sends echo message to the server.
    /// </summary>
    /// <param name="request">Request.</param>
    public void EchoMessage(EchoMessageModel request)
    {
        client.Send("echo", JObject.FromObject(new
        {
            request
        }));
    }
}
