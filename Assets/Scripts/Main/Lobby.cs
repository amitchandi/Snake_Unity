using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Lobby : MonoBehaviour
{
    public TMPro.TMP_Text roomName;
    
    Room room;

    public void SetRoom(Room room)
    {
        this.room = room;

        roomName.text = room.roomName;
    }

    public void JoinRoom()
    {
        GameObject.Find("Client").GetComponent<ServerCommunication>().JoinRoom(room.id);
        GameObject.Find("Menu").GetComponent<Menu>().StartLobby();
    }
}
