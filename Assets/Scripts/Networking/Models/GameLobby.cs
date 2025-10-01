using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

[Serializable]
public class GameLobby
{
    public string id { get; set; }
    public string[] players { get; set; }
    public Dictionary<string, User> playerObjects { get; set; }
    public int wallsToStart { get; set; }

    override public string ToString()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(this);
    }

    //public static explicit operator GameLobby(JObject jsonObject)
    //{
    //    return new GameLobby()
    //    {
    //        id = (string)jsonObject["id"],
    //        players = jsonObject["players"].ToObject<string[]>(),
    //        playerObjects = jsonObject["playerObjects"].ToObject<Dictionary<string, User>>(),
    //        wallsToStart = (int)jsonObject["wallsToStart"],
    //    };
    //}
}

public static class MyLobby
{
    public static GameLobby Lobby = new();
}