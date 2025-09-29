using System;
using System.Collections.Generic;

[Serializable]
public class Room
{
    public string id { get; set; }
    public string roomName { get; set; }
    public bool isGameRoom { get; set; }
    public bool inGame { get; set; }
    public string ownerId { get; set; }
    public List<User> users { get; set; }
    public RoomSettings settings { get; set; }

    override public string ToString()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(this);
    }
}

[Serializable]
public class RoomSettings
{
    public int wallsToStart { get; set; }

    override public string ToString()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(this);
    }
}
