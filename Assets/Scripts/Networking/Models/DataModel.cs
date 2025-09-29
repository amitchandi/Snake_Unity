using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
/// <summary>
/// Message model.
/// </summary>
[System.Serializable]
public class DataModel
{
    public string @event;
    public JObject data;

    override public string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
