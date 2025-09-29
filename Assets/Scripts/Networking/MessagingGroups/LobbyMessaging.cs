using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Class holding lobby messages.
/// </summary>
public class LobbyMessaging : BaseMessaging
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:LobbyMessaging"/> class.
    /// </summary>
    /// <param name="client">Client.</param>
    public LobbyMessaging(ServerCommunication client) : base(client) { }

    // Register messages
    public const string Register = "register";
    public UnityAction OnConnectedToServer;

    // Echo messages
    public const string Echo = "echo";
    public UnityAction<EchoMessageModel> OnEchoMessage;

    // On Get Room
    public UnityAction<DataModel> OnGetRoom;

    // On Join Room
    public UnityAction<DataModel> OnJoinRoom;

    // On Leave Room
    public UnityAction<DataModel> OnLeaveRoom;

    // On Delete Room
    public UnityAction OnDeleteRoom;

    // On Set Ready Status
    public UnityAction<DataModel> OnSetReady;

    // On Chat Message
    public UnityAction<DataModel> OnChatMessage;

    // On Start Game
    public UnityAction OnStartGame;

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

    // On GetGameState
    public UnityAction<DataModel> OnGetGameState;

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
