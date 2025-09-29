using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class HTTPClient : IDisposable
{
    private readonly HttpClient restClient = new();
    private readonly string url;

    public HTTPClient(string url)
    {
        this.url = url;
        restClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        restClient.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
    }

    public void Dispose()
    {
        restClient.Dispose();
    }

    public async Task<string> GetToken()
    {
        var stringTask = restClient.GetStringAsync(url + "/getToken");

        return await stringTask;
    }

    /**
     * returns the user object as JSON
     */
    public async Task<string> GetUser(string deviceId)
    {
        var stringTask = await restClient.GetStringAsync(url + "/getUser/" + deviceId);

        if (stringTask == "")
            return null;
        else
            return stringTask;
    }

    /**
     * returns user id
     */
    public async Task<string> CreateUser(string deviceId, string name)
    {
        var res = await restClient.PostAsync(url + "/createUser",
            new StringContent(JsonConvert.SerializeObject(new
            {
                deviceId,
                name
            }), Encoding.UTF8, "application/json"));

        return await res.Content.ReadAsStringAsync();
    }

    /**
     * returns list of Room objects as JSON
     */
    public async Task<string> GetRooms()
    {
        var stringTask = restClient.GetStringAsync(url + "/getRooms");

        return await stringTask;
    }

    /**
     * returns Room object as JSON
     */
    public async Task<string> CreateRoom(string roomName, bool isGameRoom, string ownerId, int wallsToStart)
    {
        var res = await restClient.PostAsync(url + "/createRoom",
            new StringContent(JsonConvert.SerializeObject(new
            {
                roomName,
                isGameRoom,
                ownerId,
                settings = new
                {
                    wallsToStart
                }
            }), Encoding.UTF8, "application/json"));

        return await res.Content.ReadAsStringAsync();
    }

    public async Task<string> DeleteRoom(string roomId)
    {
        var res = await restClient.DeleteAsync(url + "/deleteUser/" + roomId);

        return await res.Content.ReadAsStringAsync();
    }

    public async Task<bool> UpdateUserName(string email, string newUsername)
    {
        var res = await restClient.PutAsync(url + "/updateUsername", ToJSON(new
        {
            email,
            newUsername
        }));

        return await res.Content.ReadAsStringAsync() == "true";
    }

    public async Task<(bool valid, string content)> Login(string email, string password)
    {
        var res = await restClient.PostAsync(url + "/login", ToJSON(new
        {
            email,
            password
        }));

        var str = await res.Content.ReadAsStringAsync();
        Debug.Log(str);
        if (res.StatusCode == HttpStatusCode.OK)
        {
            return (true, str);
        }
        else if (res.StatusCode == HttpStatusCode.BadRequest)
        {
            return (false, "Invalid username or password.");
        }

        return (false, "something went wrong");
    }

    public async Task<bool> Register(string email, string username, string password)
    {
        var res = await restClient.PostAsync(url + "/createUser", ToJSON(new
        {
            email,
            username,
            password
        }));

        var str = await res.Content.ReadAsStringAsync();
        Debug.Log(str);
        if (res.StatusCode == HttpStatusCode.Created)
        {
            return true;
        }

        return false;
    }

    private StringContent ToJSON(object o)
    {
        var serial = JsonConvert.SerializeObject(o);
        return new StringContent(serial, Encoding.UTF8, "application/json");
    }

}
