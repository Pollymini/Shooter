using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] TMP_Text text;
 
    RoomInfo info;
    
    public void SetUp(RoomInfo _info)
    {
        info = _info;
        text.text = info.Name;
    }
    public void OnClick()
    {
        LobbyCreation.Instance.JoinRoom(info);
        
    }
}

   
