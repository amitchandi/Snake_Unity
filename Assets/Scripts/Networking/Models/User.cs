using System;

[Serializable]
public class User
{
    public string Id { get; set; }
    public string DeviceId { get; set; }
    public string Name { get; set; }
    public bool IsReady { get; set; }
    public UserState State { get; set; }
    public int Wins { get; set; }

    override public string ToString()
    {
        return Newtonsoft.Json.JsonConvert.SerializeObject(this);
    }
}

public enum UserState
{
    alive = 0,
    dead = 1,
}