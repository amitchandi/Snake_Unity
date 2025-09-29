using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UserCard : MonoBehaviour
{
    User user;
    public Image avatar;
    public TMPro.TMP_Text username;
    public TMPro.TMP_Text wins;
    public GameObject status;

    public void SetUser(User user)
    {
        this.user = user;
        username.SetText(user.Name);
        wins.SetText("Wins: " + user.Wins);
        name = "UserCard:" + user.Id;
        ChangeReadyStatus(user.IsReady);
        //avatar.sprite = user.Avatar
    }

    public void ChangeReadyStatus(bool isReady)
    {
        user.IsReady = isReady;
        if (user.IsReady)
            status.GetComponent<Image>().color = Color.green;
        else
            status.GetComponent<Image>().color = Color.red;
    }

    public string GetUserId()
    {
        return user.Id;
    }

    public bool IsReady()
    {
        return user.IsReady;
    }
}
