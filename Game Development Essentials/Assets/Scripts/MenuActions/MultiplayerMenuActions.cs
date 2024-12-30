
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class MultiplayerMenuActions : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
{
    [Serializable]
    public class ServerFoundEvent : UnityEvent<IPEndPoint, DiscoveryResponseData>
    {
    };

    private string ServerName;
    private string PlayerName = "Jort"; // Default name, to be set by the user
    
    public GameObject hostnamePrefab;
    public GameObject hostnameSessionsParent;

    public Lobby lobby;
    // public NetworkList<int> playersInLobby;
    private NetworkManager m_NetworkManager;
    public ServerFoundEvent OnServerFound;
    Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new();
    Dictionary<IPAddress, Button> hostnameButtons = new();
    
    public void PlayMultiplayerGame(){
        StopDiscovery();
        lobby.StartGame();
    }

    private void Start(){
        m_NetworkManager = NetworkManager.Singleton;
        // reset on load

        ServerName = PlayerName+"'s Lobby";
        OnServerFound.AddListener(AddServer);
        OnServerFound.AddListener(UpdateHosts);
        m_NetworkManager.OnClientConnectedCallback +=  lobby.HandleClientConnectedGUI;
        m_NetworkManager.OnClientDisconnectCallback += lobby.HandleClientDisconnectedGUI;
    }


    public void HostLobby()
    {

        // Set the UnityTransport connection data to use the local IP address
        UnityTransport transport = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
        string localIPAddress = GetLocalIPAddress();
        ushort port = transport.ConnectionData.Port;

        if (IsUdpPortInUse(port))
        {
            Debug.LogError($"Port {port} is already in use. Cannot start host.");
            return; // Prevent starting the host if the port is in use
        }
        transport.SetConnectionData(localIPAddress, port);

        m_NetworkManager.StartHost();
        StartServer(port);

        // lobby GUI
        lobby.ChangeLobbyName(ServerName);
        lobby.GoToLobbyScreen();
    }

    public bool IsUdpPortInUse(int port)
    {
        try
        {
            using (UdpClient udpClient = new UdpClient(port))
            {
                // If we can bind to the port, it's not in use
                return false;
            }
        }
        catch (SocketException)
        {
            // If we catch a SocketException, the port is likely in use
            return true;
        }
    }

    private string GetLocalIPAddress()
    {
        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus == OperationalStatus.Up)
            {
                foreach (var address in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    // Ensure we are using an IPv4 address that is not a loopback address
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address.Address))
                    {
                        return address.Address.ToString();
                    }
                }
            }
        }
        return string.Empty; // Return empty string if no valid address is found
    }

    public void AddServer(IPEndPoint sender, DiscoveryResponseData response)
    {
        discoveredServers[sender.Address] = response;
    }

    private void UpdateHosts(IPEndPoint sender, DiscoveryResponseData response){
        GameObject GO = Instantiate(hostnamePrefab, hostnameSessionsParent.transform);
        RectTransform GOrect = GO.GetComponent<RectTransform>();

        // // Adjust the localPosition to move it based on its height
        GOrect.transform.localPosition = new Vector3(
            GOrect.transform.localPosition.x,
            GOrect.transform.localPosition.y - GOrect.rect.height * hostnameButtons.Count,
            GOrect.transform.localPosition.z
        );

        // Update the button text and behavior
        Button newButton = GO.GetComponent<Button>();
        TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
        buttonText.text = response.ServerName;

        lobby.ChangeLobbyName(response.ServerName);

        newButton.onClick.AddListener(() =>
        {
            UnityTransport transport = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
            transport.SetConnectionData(sender.Address.ToString(), response.Port);
            ConnectToLobby();
        });

        hostnameButtons[sender.Address] = newButton;
    }

    public void StartClientDiscovery(){
        StartClient();
        ClientBroadcast(new DiscoveryBroadcastData());
    }

    public void ConnectToLobby()
    {       
        m_NetworkManager.StartClient();

        lobby.EnableLobbyScreen();
    }

    public void DisconnectGUI(){
        Disconnect(m_NetworkManager.LocalClientId);
    }
    public void Disconnect(ulong clientid)
    {
        // Stop discovery if running
        if (IsRunning)
        {
            StopDiscovery();
        }

        // Clear discovered servers and buttons
        discoveredServers.Clear();
        foreach (var button in hostnameButtons)
        {
            Destroy(button.Value.gameObject);
        }
        hostnameButtons.Clear();

        // Shut down network manager
        if (m_NetworkManager.IsHost)
        {
            lobby.DisconnectClientServerRpc(clientid, true);
            lobby.DisableLobbyScreen();

            // Ensure transport is properly stopped
            UnityTransport transport = (UnityTransport)m_NetworkManager.NetworkConfig.NetworkTransport;
            transport.Shutdown();

            m_NetworkManager.Shutdown();
        }
        else
        {
            lobby.Disconnect();
        }
    }


    public void RefreshHostList(){
        if(!IsRunning) return;

        discoveredServers.Clear();
        foreach(var button in hostnameButtons){
            Destroy(button.Value.gameObject);
        }
        hostnameButtons.Clear();
        ClientBroadcast(new DiscoveryBroadcastData());
    }


    protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response)
    {
        response = new DiscoveryResponseData()
        {
            ServerName = ServerName,
            Port = ((UnityTransport) m_NetworkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
        };
        return true;
    }

    protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response)
    {
        OnServerFound.Invoke(sender, response);
    }


    public async Task WaitAsync(int seconds)
    {
        await WaitMsAsync(seconds * 1000);
    }

    public async Task WaitMsAsync(float ms)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while ((float)stopwatch.Elapsed.TotalMilliseconds < ms)
        {
            await Task.Delay(1);
        }
    }

}
