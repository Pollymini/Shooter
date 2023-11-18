using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
using UnityEngine.UIElements;
using Photon.Realtime;
using System.Linq;

public class LobbyCreation : MonoBehaviourPunCallbacks
{

    public static LobbyCreation Instance;
    [SerializeField] TMP_InputField roomNameInputFieald;
    [SerializeField] TMP_Text roomNameText;

    [SerializeField] Transform roomListContent;
    [SerializeField] GameObject roomListItemPrefab;
    [SerializeField] Transform playerListContent;
    [SerializeField] GameObject playerListItemPrefab;


    [SerializeField] GameObject roommenu;
    [SerializeField] GameObject CTS;
    [SerializeField] GameObject startGameButton; 



    private void Awake()
    {
        Instance = this;    
    }

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnConnectedToMaster()
    {
        Debug.Log("Joinned to Master");

        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
        
    }
    public override void OnJoinedLobby()
    {
        Debug.Log("Joinned to lobby");
        PhotonNetwork.NickName = "Player" + Random.Range(0, 1000).ToString("0000");
    }
    public void CreateRoom()
    {
        if(string.IsNullOrEmpty(roomNameInputFieald.text))
        {
            return;
        }

        PhotonNetwork.CreateRoom(roomNameInputFieald.text);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(" Joinned to room");
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < players.Count(); i++)
        {
            Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(players[i]);
        }
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }
    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        
    }
    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        roommenu.SetActive(false);
    }
    public void JoinRoom(RoomInfo info)
    {
        PhotonNetwork.JoinRoom(info.Name);

        if(CTS != null)
        {
            CTS.SetActive(false);
            roommenu.SetActive(true);
        }
        else
            roommenu.SetActive(true);
       

       

    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
      
    }
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach(Transform transform in roomListContent)
        {
            Destroy(transform.gameObject);
        }

      for(int i = 0; i < roomList.Count; i++)
        {
            Instantiate(roomListItemPrefab, roomListContent).GetComponent<RoomListItem>().SetUp(roomList[i]);
        }
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(playerListItemPrefab, playerListContent).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }
    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }

}
