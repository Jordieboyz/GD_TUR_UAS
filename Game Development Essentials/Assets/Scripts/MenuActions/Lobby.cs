
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lobby : NetworkBehaviour
{

    public GameObject Hosts;
    public GameObject LobbyNameGUI;

    public GameObject LobbyGUI;
    public GameObject ClientButton;
    public GameObject HostButton;
    public GameObject LobbyTilemap;
    public GameObject HostsTilemap;
    public GameObject TitleTilemap;
    public GameObject MultiplayerMenu;
    public GameObject pInLobbyParent;
    public GameObject pInLobbyPrefab;
    
    public bool IsReady = false;
    private Dictionary<ulong, GameObject> joinedPlayers = new();
    private Dictionary<ulong, bool> playerReadyStatus = new();

    public void HandleClientConnectedGUI(ulong cID){
        UpdateGUIServerRpc(cID);
        IsReady = IsHost;
        UpdateReadyStatusServerRpc(NetworkManager.LocalClientId, IsReady);
    }
        
    public async void HandleClientDisconnectedGUI(ulong clientID){
        foreach(var cId in joinedPlayers){
            Destroy(cId.Value.gameObject);
        }
        joinedPlayers.Clear();
        
        while(NetworkManager.ConnectedClientsIds.Contains(clientID)){
            await Task.Yield();
        }
        UpdateGUIServerRpc(clientID);
        if(!IsHost)
            DisableLobbyScreen();
    }

    public void ChangeLobbyName(string lobbyName){
        LobbyNameGUI.GetComponent<TMP_Text>().text = lobbyName;
    }

    public void DisableLobbyScreen(){
        TitleTilemap?.SetActive(true);
        LobbyGUI.SetActive(false);
        LobbyTilemap.SetActive(false);
        MultiplayerMenu.SetActive(true);
    }

    public void GoToLobbyScreen(){
        MultiplayerMenu.SetActive(false);
        LobbyGUI.SetActive(true);
        TitleTilemap.SetActive(false);
        LobbyTilemap.SetActive(true);
        if(!NetworkManager.IsHost){
            ClientButton.SetActive(true);
            HostButton.SetActive(false);
        } else {
            ClientButton.SetActive(false);
            HostButton.SetActive(true);
        }
    }
    public void EnableLobbyScreen(){

        if(NetworkManager.IsClient){
            ClientButton.SetActive(true);
            HostButton.SetActive(false);
        } else {
            ClientButton.SetActive(false);
            HostButton.SetActive(true);
        }
        Hosts.SetActive(false);
        LobbyGUI.SetActive(true);
        LobbyTilemap.SetActive(true);
        HostsTilemap.SetActive(false);
    }

    private void UpdateLobbyGUI(ulong clientId){
        // this basically means, remove the disconnected players
        if(!NetworkManager.ConnectedClientsIds.Contains(clientId)){
            if(joinedPlayers.ContainsKey(clientId)){
                Destroy(joinedPlayers[clientId].gameObject);
                joinedPlayers.Remove(clientId);
            }
        }

        GameObject curr;
        for(int i = 0; i < NetworkManager.ConnectedClientsIds.Count; i++){ 
            ulong cId = NetworkManager.ConnectedClientsIds[i];

            if(!joinedPlayers.ContainsKey(cId)){
                // if we already joined and just want to update the lobby
                curr = Instantiate(pInLobbyPrefab, pInLobbyParent.transform);
                curr.transform.GetChild(0).GetComponent<TMP_Text>().text = "Player "+cId;
                joinedPlayers[cId] = curr;
            } else {
                curr = joinedPlayers[cId];
            }

            RectTransform currRect = curr.GetComponent<RectTransform>();
            currRect.transform.localPosition = new Vector3(
                currRect.transform.localPosition.x,
                -25 - (currRect.rect.height * i),
                currRect.transform.localPosition.z
            );
        }
    }

    public void StartGame(){
        // Check if all players are ready
        if (NetworkManager.ConnectedClientsIds.All(id => playerReadyStatus.ContainsKey(id) && playerReadyStatus[id]))
        {
            NetworkManager.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
        } else {
            Debug.LogWarning("Not all players are ready!");
        }
    }

    public void Disconnect(){
        DisconnectClientServerRpc(NetworkManager.LocalClientId, false);
    }
    public void ToggleReadyStatus()
    {
        IsReady = ! IsReady;
        UpdateReadyStatusServerRpc(NetworkManager.LocalClientId, IsReady);
    }

    private void ReadyUp(ulong cID, bool isReady){
        if(isReady == true){ 
            joinedPlayers[cID].gameObject.transform.GetChild(1).GetComponent<Image>().color = Color.green;
        } else{ 
            joinedPlayers[cID].gameObject.transform.GetChild(1).GetComponent<Image>().color = Color.red;
        }
    }

    /********************************************************************
        Host - Client Logic
            Tell the host that the current client is done witht the game.
            
    **********************************************************************/
    [ServerRpc(RequireOwnership = false)]
    public void UpdateGUIServerRpc(ulong clientID)
    {
        UpdateGUIClientRpc(clientID);
    }

    [ClientRpc]
    public void UpdateGUIClientRpc(ulong clientID)
    {   
        UpdateLobbyGUI(clientID);
    }


    [ServerRpc(RequireOwnership = false)]
    private void UpdateReadyStatusServerRpc(ulong clientId, bool isReady)
    {
        if (!playerReadyStatus.ContainsKey(clientId))
        {
            playerReadyStatus[clientId] = false;
        }

        playerReadyStatus[clientId] = isReady;

        UpdateReadyStatusClientRpc(clientId, isReady);
    }

    [ClientRpc]
    private void UpdateReadyStatusClientRpc(ulong clientId, bool isReady)
    {
        if (joinedPlayers.ContainsKey(clientId))
        {
            ReadyUp(clientId, isReady);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DisconnectClientServerRpc(ulong clientID, bool shutdown, ServerRpcParams rpcParams = default)
    {   
        if(IsHost && shutdown && clientID == 0){
            DisconnectClientClientRpc();
        }
        // Check if the clientID matches the sender's client ID
        if (rpcParams.Receive.SenderClientId == clientID || IsHost)
        {
            NetworkManager.DisconnectClient(clientID);
            DisconnectClientClientRpc();
        }
        else
        {
            Debug.LogWarning($"Client {rpcParams.Receive.SenderClientId} attempted to disconnect client {clientID}, but was denied.");
        }
    }

    [ClientRpc]
    public void DisconnectClientClientRpc()
    {
        if(!IsHost){
            NetworkManager.Shutdown();
            DisableLobbyScreen();
        }
    }
}