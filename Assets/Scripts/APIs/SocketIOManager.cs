using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;

public class SocketIOManager : MonoBehaviour
{
    [Header("User Token")]
    [SerializeField] private string _testToken;

    [Header("Managers")]
    [SerializeField] private SlotBehaviour _slotBehaviour;
    [SerializeField] private UIManager _uiManager;
    protected string SocketURI = null;
    protected string TestSocketURI = "http://localhost:5001/";
    // protected string gameID = "SL-LLL";
    protected string gameID = "";
    private SocketManager manager;
    private const int maxReconnectionAttempts = 6;
    private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);
    private string myAuth = null;
    internal GameData initialData = null;
    internal UIData initUIData = null;
    internal GameData resultData = null;
    internal PlayerData playerdata = null;
    internal List<string> bonusdata = null;
    internal bool isResultdone = false;
    internal bool SetInit = false;

    private void Start()
    {
        SetInit = false;
        OpenSocket();
    }

    void ReceiveAuthToken(string jsonData)
    {
        Debug.Log("Received Auth Token Data: " + jsonData);
        // Parse the JSON data
        var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
        SocketURI = data.socketURL;
        myAuth = data.cookie;
    }

    private void OpenSocket()
    {
        // Create and setup SocketOptions
        SocketOptions options = new SocketOptions();
        options.ReconnectionAttempts = maxReconnectionAttempts;
        options.ReconnectionDelay = reconnectionDelay;
        options.Reconnection = true;

        Application.ExternalCall("window.parent.postMessage", "authToken", "*");

#if UNITY_WEBGL && !UNITY_EDITOR
        Application.ExternalEval(@"
            window.addEventListener('message', function(event) {
                if (event.data.type === 'authToken') {
                    var combinedData = JSON.stringify({
                        cookie: event.data.cookie,
                        socketURL: event.data.socketURL
                    });
                    // Send the combined data to Unity
                    SendMessage('SocketManager', 'ReceiveAuthToken', combinedData);
                }});");
        StartCoroutine(WaitForAuthToken(options));
#else
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = _testToken,
                gameId = gameID
            };
        };
        options.Auth = authFunction;
        // Proceed with connecting to the server
        SetupSocketManager(options);
#endif
    }

    private IEnumerator WaitForAuthToken(SocketOptions options)
    {
        // Wait until myAuth is not null
        while (myAuth == null)
        {
            Debug.Log("My Auth is null");
            yield return null;
        }
        while (SocketURI == null)
        {
            Debug.Log("My Socket is null");
            yield return null;
        }
        Debug.Log("My Auth is not null");
        // Once myAuth is set, configure the authFunction
        Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
        {
            return new
            {
                token = myAuth,
                gameId = gameID
            };
        };
        options.Auth = authFunction;

        Debug.Log("Auth function configured with token: " + myAuth);

        // Proceed with connecting to the server
        SetupSocketManager(options);
    }

    private void SetupSocketManager(SocketOptions options)
    {
        // Create and setup SocketManager
#if UNITY_EDITOR
        this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
        this.manager = new SocketManager(new Uri(SocketURI), options);
#endif
        // Set subscriptions
        this.manager.Socket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
        this.manager.Socket.On<string>(SocketIOEventTypes.Disconnect, OnDisconnected);
        this.manager.Socket.On<string>(SocketIOEventTypes.Error, OnError);
        this.manager.Socket.On<string>("message", OnListenEvent);
        this.manager.Socket.On<bool>("socketState", OnSocketState);
        this.manager.Socket.On<string>("internalError", OnSocketError);
        this.manager.Socket.On<string>("alert", OnSocketAlert);
        this.manager.Socket.On<string>("AnotherDevice", OnSocketOtherDevice);
        // Start connecting to the server
    }

    // Connected event handler implementation
    void OnConnected(ConnectResponse resp)
    {
        Debug.Log("Socket Connected!");
        SendPing();
    }

    private void OnDisconnected(string response)
    {
        Debug.Log("Socket Disconnected!");
        StopAllCoroutines();
        _uiManager.DisconnectionPopup();
    }

    private void OnError(string response)
    {
        Debug.LogError("Socket Error!: " + response);
    }

    private void OnListenEvent(string data)
    {
        ParseResponse(data);
    }

    private void OnSocketState(bool state)
    {
        Debug.Log("Socket State: " + state);
    }

    private void OnSocketError(string data)
    {
        Debug.Log("Socket Error!: " + data);
    }

    private void OnSocketAlert(string data)
    {
        Debug.Log("Socket Alert!: " + data);
    }

    private void OnSocketOtherDevice(string data)
    {
        Debug.Log("Received Device Error with data: " + data);
        _uiManager.ADfunction();
    }

    private void SendPing()
    {
        InvokeRepeating("AliveRequest", 0f, 3f);
    }

    private void AliveRequest()
    {
        SendDataWithNamespace("YES I AM ALIVE");
    }

    private void SendDataWithNamespace(string eventName, string json = null)
    {
        // Send the message
        if (this.manager.Socket != null && this.manager.Socket.IsOpen)
        {
            if (json != null)
            {
                this.manager.Socket.Emit(eventName, json);
                Debug.Log("JSON data sent: " + json);
            }
            else
            {
                this.manager.Socket.Emit(eventName);
            }
        }
        else
        {
            Debug.LogWarning("Socket is not connected.");
        }
    }

    public void CloseSocket()
    {
        SendDataWithNamespace("EXIT");
    }

    private void ParseResponse(string jsonObject)
    {
        Debug.Log(jsonObject);
        Root myData=JsonConvert.DeserializeObject<Root>(jsonObject);

        string id = myData.id;

        switch (id)
        {
            case "InitData":
                {
                    initialData = myData.message.GameData;
                    initUIData = myData.message.UIData;
                    playerdata = myData.message.PlayerData;
                    bonusdata = myData.message.BonusData;
                    if (!SetInit)
                    {
                        SetInit = true;
                        Application.ExternalCall("window.parent.postMessage", "OnEnter", "*");
                        PopulateSlotGame();
                    }
                    else
                    {
                        _uiManager.InitialiseUIData(initUIData.paylines);
                    }
                    break;
                }
            case "ResultData":
                {
                    resultData = myData.message.GameData;
                    playerdata = myData.message.PlayerData;
                    isResultdone = true;
                    break;
                }
            case "ExitUser":
                {
                    if (this.manager != null)
                    {
                        Debug.Log("Dispose my Socket");
                        this.manager.Close();
                    }
                    Application.ExternalCall("window.parent.postMessage", "onExit", "*");
                    break;
                }
        }
    }

    private void PopulateSlotGame()
    {
        _slotBehaviour.SetInitialUI();
        _uiManager.InitialiseUIData(initUIData.paylines);
    }

    internal void AccumulateResult(double currBet)
    {
        isResultdone = false;

        MessageData message = new MessageData();
        message.data = new BetData();
        message.data.currentBet = currBet;
        message.data.spins = 1;
        message.data.currentLines = 15;
        message.id = "SPIN";

        string json = JsonConvert.SerializeObject(message); // Serialize message data to JSON
        SendDataWithNamespace("message", json);
    }
}