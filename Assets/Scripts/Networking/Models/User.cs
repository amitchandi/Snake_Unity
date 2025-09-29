using System;

[Serializable]
public class User
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public bool IsReady { get; set; }
    public UserState State { get; set; }
    public int Wins { get; set; }
    public int GamesPlayed { get; set; }

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