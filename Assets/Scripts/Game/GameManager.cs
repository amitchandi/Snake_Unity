//using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum Special
{
    zoom,
    slow,
    invincible,
    //confuse,
    //shrink,
}

public enum Directions
{
    LEFT,
    RIGHT,
    UP,
    DOWN
}

static class DirectionsExtensions
{
    public static Directions Opposite(this Directions direction)
    {
        Directions opposite = Directions.DOWN;
        switch (direction)
        {
            case Directions.LEFT:
                opposite = Directions.RIGHT;
                break;
            case Directions.RIGHT:
                opposite = Directions.LEFT;
                break;
            case Directions.UP:
                opposite = Directions.DOWN;
                break;
            case Directions.DOWN:
                opposite = Directions.UP;
                break;
        }
        return opposite;
    }
}

public class GameManager : MonoBehaviour
{
    public GameObject gameBoard;
    public GameObject emptyTile;
    public GameObject snakeTile;
    public GameObject wallTile;
    public GameObject pelletTile;
    public GameObject specialTile;

    public AudioSource zoomSound;
    public AudioSource gameTheme;
    public AudioSource deathSound;
    public AudioSource munchSound;
    public AudioSource crunchSound;
    public AudioSource specialSound;
    public AudioSource gameWinSound;
    public AudioSource wallDropSound;

    public GameObject toast;

    public MyInputMaster controls;

    public TMP_Text spectateName;
    public TMP_Text PlayerCount;

    GameObject messages;

    GameObject client;
    GameObject settings;

    User user;
    Room room;

    List<int> snakePos;
    List<int> wallPos;
    List<GameObject> tiles;
    int pellet = -1;
    int special = -1;
    System.Random random = new System.Random();
    Directions direction = Directions.DOWN;
    Directions nextDirection = Directions.DOWN;
    bool isGameRunning = false;
    float speed = 0.3f;
    float elapsedTime = 0f;

    bool InSpecial = false;
    bool InZoom = false;
    bool InSlow = false;
    bool InInvincible = false;
    bool CanSpawnSpecial = true;

    GameState gameState = new GameState();
    GameState spectatorGameState;

    #region [Unity Message]
    // Start is called before the first frame update
    void Start()
    {
        messages = Instantiate(toast, GameObject.Find("Panel").transform);

        spectateName.gameObject.SetActive(false);

        tiles = new List<GameObject>();
        snakePos = new List<int>()
        {
            46,66,86,106
        };
        wallPos = new List<int>();
        GetPellet();

        controls = new MyInputMaster();
        controls.Player.Movement.performed += ctx => ChangeDirection(FromVector2(ctx.ReadValue<Vector2>()));
        controls.Enable();
        controls.Player.SpectateControls.Disable();
        controls.Player.SpectateControls.performed += ctx => ChangeSpectating(ctx.ReadValue<float>());

        StartCoroutine(LoadGame());
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isGameRunning)
        {
            elapsedTime += Time.fixedDeltaTime;
            if (elapsedTime > speed)
            {
                elapsedTime -= speed;
                HandleInputs(out int removed);
                HandleGameTiles();
                UpdateGameObjects(removed);
                SendSetGameState();
            }
        }
        else
        {
            if (user != null && user.State == UserState.dead)
            {
                if (spectatorGameState == null)
                {
                    User su = room.users.Find(u =>
                    {
                        return u.Id != user.Id && u.Name != "Mama Snake";
                    });
                    client.GetComponent<ServerCommunication>().SendGetGameState(su.Id);
                }
                else
                {
                    for (int i = 0; i < spectatorGameState.tiles.Count; i++)
                    {
                        Tile tile = spectatorGameState.tiles[i];
                            
                        if (tile.tileType == TileType.EMPTY)
                        {
                            tiles[i].GetComponent<Image>().sprite = emptyTile.GetComponent<Image>().sprite;
                            tiles[i].GetComponent<TileObject>().tile.tileType = TileType.EMPTY;
                        }
                        else if (tile.tileType == TileType.PELLET)
                        {
                            tiles[i].GetComponent<Image>().sprite = pelletTile.GetComponent<Image>().sprite;
                            tiles[i].GetComponent<TileObject>().tile.tileType = TileType.PELLET;
                        }
                        else if (tile.tileType == TileType.SNAKE)
                        {
                            tiles[i].GetComponent<Image>().sprite = snakeTile.GetComponent<Image>().sprite;
                            tiles[i].GetComponent<TileObject>().tile.tileType = TileType.SNAKE;
                        }
                        else if (tile.tileType == TileType.SPECIAL)
                        {
                            tiles[i].GetComponent<Image>().sprite = specialTile.GetComponent<Image>().sprite;
                            tiles[i].GetComponent<TileObject>().tile.tileType = TileType.SPECIAL;
                        }
                        else if (tile.tileType == TileType.WALL)
                        {
                            tiles[i].GetComponent<Image>().sprite = wallTile.GetComponent<Image>().sprite;
                            tiles[i].GetComponent<TileObject>().tile.tileType = TileType.WALL;
                        }
                    }
                    client.GetComponent<ServerCommunication>().SendGetGameState(spectatorGameState.userId);
                }
            }
        }
    }
    #endregion

    #region [Events]
    void OnGetRoom(DataModel message)
    {
        room = message.data["room"].ToObject<Room>();
    }
    
    void OnDie(DataModel message)
    {
        messages.GetComponent<Toast>().ShowToast(message.data["args"]["username"].ToObject<string>() + " Has Died\nDummy", 2);
        deathSound.Play();
        client.GetComponent<ServerCommunication>().RetrieveUser();
        client.GetComponent<ServerCommunication>().GetRoom(room.id);
    }

    void OnWin(DataModel message)
    {
        if (message.data["args"]["userId"].ToObject<string>() == client.GetComponent<ServerCommunication>().GetUser().Id)
        {
            isGameRunning = false;
            client.GetComponent<ServerCommunication>().AddWin();
        }
        messages.GetComponent<Toast>().queue.Clear();
        messages.GetComponent<Toast>().StopAllCoroutines();
        messages.GetComponent<Toast>().ShowToast(message.data["args"]["username"].ToObject<string>() + " Has Won The Game!\n;)", 2);
        gameWinSound.Play();
    }

    void OnReset(DataModel message)
    {
        StartCoroutine(ResetLobby());
    }

    void OnEatPellet(DataModel message)
    {
        if (message.data["args"]["userId"].ToString() != user.Id)
        {
            ReceiveEat(message.data["args"]["magnitude"].ToObject<int>());
        }
    }

    void OnZoom(DataModel message)
    {
        if (message.data["args"]["userId"].ToString() != user.Id)
        {
            ReceiveZoom();
        }
    }

    void OnSlow(DataModel message)
    {
        if (message.data["args"]["userId"].ToString() == user.Id)
        {
            ReceiveSlow();
        }
    }

    void OnInvincible(DataModel message)
    {
        if (message.data["args"]["userId"].ToString() == user.Id)
        {
            ReceiveInvincible();
        }
    }

    void OnGetGameState(DataModel message)
    {
        string username = message.data["username"].ToString();
        if (!spectateName.gameObject.activeInHierarchy)
            spectateName.gameObject.SetActive(true);
        if (spectateName.text != ("Spectating: " + username))
            spectateName.text = "Spectating: " + username;

        spectatorGameState = message.data["gameState"].ToObject<GameState>();
    }
    #endregion

    #region [Public Methods]
    public void ChangeSpectating(float val)
    {
        int index = room.users.FindIndex(user =>
        {
            return user.Id == spectatorGameState.userId;
        });

        if (val == 1)
        {
            if (index == room.users.Count - 1)
                index = 0;
            else
                index++;
        }
        else
        {
            if (index == 0)
                index = room.users.Count - 1;
            else
                index--;
        }

        if (room.users[index].Name == "Mama Snake")
            ChangeSpectating(val);
        else
            spectatorGameState.userId = room.users[index].Id;
    }

    public void ChangeDirection(Directions dir)
    {
        if (nextDirection.Opposite() != dir)
            nextDirection = dir;
    }

    public void SwipeDir(string dir)
    {
        ChangeDirection((Directions)Enum.Parse(typeof(Directions), dir));
    }

    public static Directions FromVector2(Vector2 v2)
    {
        if (v2.x == -1f && v2.y == 0f)
            return Directions.LEFT;
        else if (v2.x == 1f && v2.y == 0f)
            return Directions.RIGHT;
        else if (v2.x == 0f && v2.y == -1f)
            return Directions.DOWN;
        else if (v2.x == 0f && v2.y == 1f)
            return Directions.UP;

        return Directions.DOWN;
    }
    #endregion

    #region [Private Methods]
    void StartGame()
    {
        //StartCoroutine(GameLoop());
        messages.GetComponent<Toast>().ShowToast("Start SSSSlitherin", 2);
        isGameRunning = true;
    }

    void UpdateGameObjects(int removed)
    {
        tiles[removed].GetComponent<Image>().sprite = emptyTile.GetComponent<Image>().sprite;
        tiles[removed].GetComponent<TileObject>().tile.tileType = TileType.EMPTY;

        foreach (int s in snakePos)
        {
            tiles[s].GetComponent<Image>().sprite = snakeTile.GetComponent<Image>().sprite;
            tiles[s].GetComponent<TileObject>().tile.tileType = TileType.SNAKE;
        }

        tiles[pellet].GetComponent<Image>().sprite = pelletTile.GetComponent<Image>().sprite;
        tiles[pellet].GetComponent<TileObject>().tile.tileType = TileType.PELLET;

        foreach (int w in wallPos)
        {
            tiles[w].GetComponent<Image>().sprite = wallTile.GetComponent<Image>().sprite;
            tiles[w].GetComponent<TileObject>().Shake();
            tiles[w].GetComponent<TileObject>().tile.tileType = TileType.WALL;
        }

        if (special > -1)
        {
            tiles[special].GetComponent<Image>().sprite = specialTile.GetComponent<Image>().sprite;
            tiles[special].GetComponent<TileObject>().tile.tileType = TileType.SPECIAL;
        }
    }

    void HandleInputs(out int removed)
    {
        removed = snakePos[0];
        if (direction.Opposite() == nextDirection)
            nextDirection = direction;
        else
            direction = nextDirection;

        snakePos.RemoveAt(0);

        int pos;
        if (direction == Directions.DOWN)
        {
            pos = snakePos[snakePos.Count - 1] + 20;
            if (pos > 299)
                pos = snakePos[snakePos.Count - 1] + 20 - 300;
        }
        else if (direction == Directions.UP)
        {
            pos = snakePos[snakePos.Count - 1] - 20;
            if (pos < 0)
                pos = snakePos[snakePos.Count - 1] - 20 + 300;
        }
        else if (direction == Directions.LEFT)
        {
            pos = snakePos[snakePos.Count - 1] - 1;
            if ((pos + 1) % 20 == 0)
                pos = snakePos[snakePos.Count - 1] + 19;
        }
        else // RIGHT
        {
            pos = snakePos[snakePos.Count - 1] + 1;
            if (pos % 20 == 0 || pos == 0)
                pos = snakePos[snakePos.Count - 1] - 19;
        }
        snakePos.Add(pos);
    }

    void HandleGameTiles()
    {
        if (special == -1 && CanSpawnSpecial)
            GetSpecial();

        if (snakePos[snakePos.Count - 1] == pellet)
            EatPellet();
        else if (snakePos[snakePos.Count - 1] == special)
            EatSpecial();
        else if (wallPos.Contains(snakePos[snakePos.Count - 1]) && InInvincible)
            EatWall(wallPos.IndexOf(snakePos[snakePos.Count - 1]));
        else if (wallPos.Contains(snakePos[snakePos.Count - 1]) || snakePos.GetRange(0, snakePos.Count - 2).Contains(snakePos[snakePos.Count - 1]))
            GameOver();
    }

    void SendSetGameState()
    {
        gameState.userId = user.Id;
        gameState.tiles = tiles.ConvertAll((GameObject go) => { return go.GetComponent<TileObject>().tile; });
        gameState.pellet = pellet;
        gameState.special = special;
        gameState.direction = direction;
        gameState.nextDirection = nextDirection;
        gameState.isGameRunning = isGameRunning;
        gameState.speed = speed;
        gameState.elapsedTime = elapsedTime;
        gameState.InSpecial = InSpecial;
        gameState.InZoom = InZoom;
        gameState.InSlow = InSlow;
        gameState.InInvincible = InInvincible;
        gameState.CanSpawnSpecial = CanSpawnSpecial;
        client.GetComponent<ServerCommunication>().SendSetGameState(gameState);
    }

    void AddWall()
    {
        int val = random.Next(300);
        if (snakePos.Contains(val) || wallPos.Contains(val) || val == special || val == pellet)
            AddWall();
        else
            wallPos.Add(val);
    }

    void GetPellet()
    {
        int val = random.Next(300);
        if (snakePos.Contains(val) || wallPos.Contains(val) || val == special)
            GetPellet();
        else
            pellet = val;
    }

    void GetSpecial()
    {
        if (random.Next(100) < 30)
        {
            int val = random.Next(300);
            if (snakePos.Contains(val) || wallPos.Contains(val) || val == pellet)
                GetSpecial();
            else
                special = val;
        }
    }

    void Grow()
    {
        int diff = snakePos[0] - snakePos[1];
        if (diff == 1 || diff == 19)
        {
            if (snakePos[0] % 19 == 0)
                snakePos.Insert(0, snakePos[0] - 19);
            else
                snakePos.Insert(0, snakePos[0] + 1);
        }
        else if (diff == -1 || diff == -19)
        {
            if (snakePos[0] % 20 == 0 || snakePos[0] == 0)
                snakePos.Insert(0, snakePos[0] + 19);
            else
                snakePos.Insert(0, snakePos[0] - 1);
        }
        else if (diff == 20 || diff == 280)
        {
            if ((snakePos[0] + 20) > 299)
                snakePos.Insert(0, snakePos[0] + 20 - 300);
            else
                snakePos.Insert(0, snakePos[0] + 20);
        }
        else if (diff == -20 || diff == -280)
        {
            if ((snakePos[0] - 20) < 0)
                snakePos.Insert(0, snakePos[0] - 20 + 300);
            else
                snakePos.Insert(0, snakePos[0] - 20);
        }
    }

    void EatWall(int wallIndex)
    {
        crunchSound.Play();
        tiles[wallPos[wallIndex]].GetComponent<Image>().color = emptyTile.GetComponent<Image>().color;
        wallPos.RemoveAt(wallIndex);
        ServerCommunication sc = client.GetComponent<ServerCommunication>();
        sc.EatPellet(snakePos.Count);
    }

    void EatPellet()
    {
        munchSound.Play();
        tiles[pellet].GetComponent<Image>().color = emptyTile.GetComponent<Image>().color;
        ServerCommunication sc = client.GetComponent<ServerCommunication>();
        sc.EatPellet(snakePos.Count);
        GetPellet();
    }

    void EatSpecial()
    {
        specialSound.Play();
        tiles[special].GetComponent<Image>().color = emptyTile.GetComponent<Image>().color;
        ServerCommunication sc = client.GetComponent<ServerCommunication>();
        special = -1;

        // TODO 
        Array specials = Enum.GetValues(typeof(Special));
        Special s = (Special)specials.GetValue(random.Next(specials.Length));
        sc.SendSpecial(s);

        switch (s)
        {
            case Special.zoom:
                messages.GetComponent<Toast>().ShowToast("Speed em Up", 2);
                break;
            case Special.slow:
                break;
            case Special.invincible:
                break;
        }

        StartCoroutine(SpecialCooldownCR());
    }

    void ReceiveEat(int magnitude = 1)
    {
        StartCoroutine(ReceiveEatCR(magnitude));
    }

    void ReceiveSlow()
    {
        if (!InSpecial)
        {
            if (InSlow)
                StopCoroutine(SlowCR());

            StartCoroutine(SlowCR());
        }
    }

    void ReceiveInvincible()
    {
        if (!InSpecial)
        {
            if (InInvincible)
                StopCoroutine(InvincibleCR());

            StartCoroutine(InvincibleCR());
        }
    }

    void ReceiveZoom()
    {
        if (!InSpecial)
        {
            if (InZoom)
                StopCoroutine(ZoomCR());

            StartCoroutine(ZoomCR());
        }
    }

    void LoadSettings()
    {
        float BGM_Volume = settings.GetComponent<Settings>().savedSettings.BGM_Volume;
        float SFX_Volume = settings.GetComponent<Settings>().savedSettings.SFX_Volume;
        gameTheme.volume = BGM_Volume * 0.5f;
        gameTheme.volume = BGM_Volume * 0.5f;

        zoomSound.volume = SFX_Volume * 0.5f;
        deathSound.volume = SFX_Volume * 0.5f;
        munchSound.volume = SFX_Volume * 0.5f;
        specialSound.volume = SFX_Volume * 0.5f;
        gameWinSound.volume = SFX_Volume * 0.5f;
        wallDropSound.volume = SFX_Volume * 0.5f;
        crunchSound.volume = SFX_Volume * 0.5f;
    }

    void GameOver()
    {
        controls.Player.Movement.Disable();
        controls.Player.SpectateControls.Enable();
        client.GetComponent<ServerCommunication>().Die();
        isGameRunning = false;
        Debug.Log("Game Over");
    }
    #endregion

    #region [Coroutines]
    IEnumerator LoadGame()
    {
        client = GameObject.Find("Client");
        settings = GameObject.Find("Settings");
        ServerCommunication sc = client.GetComponent<ServerCommunication>();
        yield return new WaitWhile(() => sc.isLoading);

        user = sc.GetUser();
        room = sc.GetRoom();

        for (int i = 0; i < room.settings.wallsToStart; i++)
            AddWall();

        sc.Lobby.OnGetRoom += OnGetRoom;
        sc.Lobby.OnEatPellet += OnEatPellet;
        sc.Lobby.OnDie += OnDie;
        sc.Lobby.OnWin += OnWin;
        sc.Lobby.OnReset += OnReset;
        sc.Lobby.OnZoom += OnZoom;
        sc.Lobby.OnSlow += OnSlow;
        sc.Lobby.OnInvincible += OnInvincible;
        sc.Lobby.OnGetGameState += OnGetGameState;

        LoadSettings();
        gameTheme.Play();

        for (int x = 0; x < 300; x++)
        {
            GameObject tile;
            if (snakePos.Contains(x))
            {
                tile = Instantiate(snakeTile, gameBoard.transform);
                tile.GetComponent<TileObject>().tile.tileType = TileType.SNAKE;
            }
            else if (pellet == x)
            {
                tile = Instantiate(pelletTile, gameBoard.transform);
                tile.GetComponent<TileObject>().tile.tileType = TileType.PELLET;
            }
            else if (wallPos.Contains(x))
            {
                tile = Instantiate(wallTile, gameBoard.transform);
                tile.GetComponent<TileObject>().tile.isNew = false;
                tile.GetComponent<TileObject>().tile.tileType = TileType.WALL;
            }
            else
            {
                tile = Instantiate(emptyTile, gameBoard.transform);
                tile.GetComponent<TileObject>().tile.tileType = TileType.EMPTY;
            }

            tile.name = "Tile[" + x + "]";
            tile.GetComponent<TileObject>().tile.position = x;
            tiles.Add(tile);
        }

        StartGame();
    }

    IEnumerator ResetLobby()
    {
        ServerCommunication sc = client.GetComponent<ServerCommunication>();

        sc.Lobby.OnGetRoom -= OnGetRoom;
        sc.Lobby.OnEatPellet -= OnEatPellet;
        sc.Lobby.OnDie -= OnDie;
        sc.Lobby.OnWin -= OnWin;
        sc.Lobby.OnReset -= OnReset;
        sc.Lobby.OnZoom -= OnZoom;
        sc.Lobby.OnInvincible -= OnInvincible;
        sc.Lobby.OnGetGameState -= OnGetGameState;

        sc.SendReadyChange(true);

        yield return new WaitForEndOfFrame();

        SceneManager.LoadSceneAsync("Lobby", LoadSceneMode.Single);
        client.GetComponent<ServerCommunication>().RetrieveUser();
    }

    IEnumerator ZoomCR()
    {
        InSpecial = true;
        zoomSound.Play();
        speed = 0.1f;
        messages.GetComponent<Toast>().ShowToast("We're Zoomin!", 2);
        InZoom = true;
        yield return new WaitForSecondsRealtime(10f);
        speed = 0.3f;
        messages.GetComponent<Toast>().ShowToast("Out of Coke Bud!", 2);
        InZoom = false;
        InSpecial = false;
        yield return new WaitForEndOfFrame();
    }

    IEnumerator SlowCR()
    {
        InSpecial = true;
        zoomSound.Play();
        speed = 0.5f;
        messages.GetComponent<Toast>().ShowToast("We're Taking it Slow", 2);
        InSlow = true;
        yield return new WaitForSecondsRealtime(10f);
        speed = 0.3f;
        messages.GetComponent<Toast>().ShowToast("Gotta Speed it Up Bud", 2);
        InSlow = false;
        InSpecial = false;
        yield return new WaitForEndOfFrame();
    }

    IEnumerator InvincibleCR()
    {
        InSpecial = true;
        //zoomSound.Play(); need sfx for this
        messages.GetComponent<Toast>().ShowToast("Shark Teeth!", 2);
        InInvincible = true;
        yield return new WaitForSecondsRealtime(10f);
        messages.GetComponent<Toast>().ShowToast("Your Teeth Fell Out!", 2);
        InInvincible = false;
        InSpecial = false;
        yield return new WaitForEndOfFrame();
    }

    IEnumerator SpecialCooldownCR()
    {
        CanSpawnSpecial = false;
        float cd = 10f;
        while (!CanSpawnSpecial)
        {
            yield return new WaitForSecondsRealtime(1f);
            cd -= 1f;
            if (cd == 0f)
                CanSpawnSpecial = true;
        }
        yield return new WaitForEndOfFrame();
    }

    IEnumerator ReceiveEatCR(int magnitude)
    {
        Grow();
        wallDropSound.Play();
        yield return new WaitWhile(() => { return wallDropSound.time < 0.93f; });
        magnitude -= 3;
        int count = (int)Math.Round(magnitude * 0.3, MidpointRounding.AwayFromZero);
        if (count < 1)
            count = 1;
        if (count > 5)
            count = 5;
        for (int i = 0; i < count; i++)
        {
            AddWall();
        }
    }
    #endregion
}
