using Newtonsoft.Json;
/// <summary>
/// Message model.
/// </summary>
[System.Serializable]
public class MessageModel
{
    public string @event;
    public string data;

    override public string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}