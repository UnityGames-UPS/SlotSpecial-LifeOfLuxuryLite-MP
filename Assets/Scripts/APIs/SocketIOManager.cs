using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;
using UnityEngine.UI;

public class SocketIOManager : MonoBehaviour
{
  [Header("User Token")]
  [SerializeField] private string TestToken;

  [Header("Managers")]
  [SerializeField] private SlotBehaviour _slotBehaviour;
  [SerializeField] private UIManager _uiManager;
  [SerializeField] internal JSFunctCalls JSManager;
  private Socket gameSocket;
  protected string NameSpace = "playground";
  protected string SocketURI = null;
  protected string TestSocketURI = "http://localhost:5000/";
  protected string gameID = "SL-LLL";
  // protected string gameID = "";
  private SocketManager manager;
  private const int maxReconnectionAttempts = 6;
  private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);
  private string myAuth = null;
  internal GameData initialData = null;
  internal UiData initUIData = null;
  internal Features features = null;
  internal Root resultData = null;
  internal Player playerdata = null;
  // internal List<string> bonusdata = null;
  internal bool isResultdone = false;
  internal bool SetInit = false;
  // private bool exited;

  [Header("Extras")]
  [SerializeField] private GameObject RaycastBlocker;
  internal List<List<int>> LineData = null; //

  [Header("Ping Pong")]
  private bool isConnected = false; //Back2 Start.       //
  private bool hasEverConnected = false;          //
  private const int MaxReconnectAttempts = 5;     //
  private const float ReconnectDelaySeconds = 2f;     //

  private float lastPongTime = 0f;      //
  private float pingInterval = 2f;     //
  private bool waitingForPong = false;     //
  private int missedPongs = 0;            // 
  private const int MaxMissedPongs = 5;       //
  private Coroutine PingRoutine; //Back2 end       //

  private void Awake()
  {
    SetInit = false;
  }

  private void Start()
  {
    OpenSocket();
  }

  void ReceiveAuthToken(string jsonData)
  {
    Debug.Log("Received Auth Token Data: " + jsonData);
    // Parse the JSON data
    var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
    SocketURI = data.socketURL;
    myAuth = data.cookie;
    NameSpace = data.nameSpace;
  }

  private void OpenSocket()
  {
    SocketOptions options = new SocketOptions(); //Back2 Start
    options.AutoConnect = false;
    options.Reconnection = false;
    options.Timeout = TimeSpan.FromSeconds(3); //Back2 end
    options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket;

#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("authToken");
        StartCoroutine(WaitForAuthToken(options));
#else
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = TestToken
      };
    };
    options.Auth = authFunction;
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
    Debug.Log("yo-yo");
    this.manager = new SocketManager(new Uri(TestSocketURI), options);
#else
        this.manager = new SocketManager(new Uri(SocketURI), options);
#endif
    if (string.IsNullOrEmpty(NameSpace) | string.IsNullOrWhiteSpace(NameSpace))
    {
      gameSocket = this.manager.Socket;
    }
    else
    {
      Debug.Log("Namespace used :" + NameSpace);
      gameSocket = this.manager.GetSocket("/" + NameSpace);
    }
    // Set subscriptions
    gameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
    // gameSocket.On<string>(SocketIOEventTypes.Disconnect, OnDisconnected);
    gameSocket.On(SocketIOEventTypes.Disconnect, OnDisconnected); //Back2 Start
    gameSocket.On<Error>(SocketIOEventTypes.Error, OnError);
    gameSocket.On<string>("game:init", OnListenEvent);
    gameSocket.On<string>("result", OnListenEvent);
    gameSocket.On<bool>("socketState", OnSocketState);
    gameSocket.On<string>("internalError", OnSocketError);
    gameSocket.On<string>("alert", OnSocketAlert);
    gameSocket.On<string>("pong", OnPongReceived); //Back2 Start
    gameSocket.On<string>("AnotherDevice", OnSocketOtherDevice);

    manager.Open();
  }

  // Connected event handler implementation
  void OnConnected(ConnectResponse resp) //Back2 Start
  {
    Debug.Log("✅ Connected to server.");

    if (hasEverConnected)
    {
      // _uiManager.CheckAndClosePopups();
    }

    isConnected = true;
    hasEverConnected = true;
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
    SendPing();
  } //Back2 end  

  private void OnDisconnected() //Back2 Start
  {
    Debug.LogWarning("⚠️ Disconnected from server.");
    isConnected = false;
    _uiManager.DisconnectionPopup();
    ResetPingRoutine();
  } //Back2 end

  private void OnPongReceived(string data) //Back2 Start
  {
    // Debug.Log("✅ Received pong from server.");
    waitingForPong = false;
    missedPongs = 0;
    lastPongTime = Time.time;
    // Debug.Log($"⏱️ Updated last pong time: {lastPongTime}");
    // Debug.Log($"📦 Pong payload: {data}");
  } //Back2 end

  private void OnError(Error err)
  {
    Debug.LogError("Socket Error Message: " + err);
#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("error");
#endif
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

  private void SendPing() //Back2 Start
  {
    ResetPingRoutine();
    PingRoutine = StartCoroutine(PingCheck());
  }

  void ResetPingRoutine()
  {
    if (PingRoutine != null)
    {
      StopCoroutine(PingRoutine);
    }
    PingRoutine = null;
  }

  private IEnumerator PingCheck()
  {
    while (true)
    {
      // Debug.Log($"🟡 PingCheck | waitingForPong: {waitingForPong}, missedPongs: {missedPongs}, timeSinceLastPong: {Time.time - lastPongTime}");

      if (missedPongs == 0)
      {
        // _uiManager.CheckAndClosePopups();
      }

      // If waiting for pong, and timeout passed
      if (waitingForPong)
      {
        if (missedPongs == 2)
        {
          // _uiManager.ReconnectionPopup();
        }
        missedPongs++;
        Debug.LogWarning($"⚠️ Pong missed #{missedPongs}/{MaxMissedPongs}");

        if (missedPongs >= MaxMissedPongs)
        {
          Debug.LogError("❌ Unable to connect to server — 5 consecutive pongs missed.");
          isConnected = false;
          _uiManager.DisconnectionPopup();
          yield break;
        }
      }

      // Send next ping
      waitingForPong = true;
      lastPongTime = Time.time;
      // Debug.Log("📤 Sending ping...");
      SendDataWithNamespace("ping");
      yield return new WaitForSeconds(pingInterval);
    }
  } //Back2 end

  private void SendDataWithNamespace(string eventName, string json = null)
  {
    // Send the message
    if (gameSocket != null && gameSocket.IsOpen)
    {
      if (json != null)
      {
        gameSocket.Emit(eventName, json);
        Debug.Log("JSON data sent: " + json);
      }
      else
      {
        gameSocket.Emit(eventName);
      }
    }
    else
    {
      Debug.LogWarning("Socket is not connected.");
    }
  }

  void CloseGame()
  {
    Debug.Log("Unity: Closing Game");
    StartCoroutine(CloseSocket());
  }

  internal IEnumerator CloseSocket() //Back2 Start
  {
    RaycastBlocker.SetActive(true);
    ResetPingRoutine();

    Debug.Log("Closing Socket");

    manager?.Close();
    manager = null;

    Debug.Log("Waiting for socket to close");

    yield return new WaitForSeconds(0.5f);

    Debug.Log("Socket Closed");

#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnExit"); //Telling the react platform user wants to quit and go back to homepage
#endif
  } //Back2 end

  //   IEnumerator WaitAndExit()                         //////////////
  //   {
  //     yield return new WaitForSeconds(2f);
  //     if (!exited)
  //     {
  //       exited = true;
  // #if UNITY_WEBGL && !UNITY_EDITOR
  //       JSManager.SendCustomMessage("onExit");
  // #endif
  //     }
  //   }

  private void ParseResponse(string jsonObject)
  {
    Debug.Log(jsonObject);
    Root myData = new();
    myData = JsonConvert.DeserializeObject<Root>(jsonObject);

    string id = myData.id;
    playerdata = myData.player;

    switch (id)
    {
      case "initData":
        {
          initialData = myData.gameData;
          initUIData = myData.uiData;
          features = myData.features;
          // bonusdata = myData.message.BonusData;
          if (!SetInit)
          {
            SetInit = true;
            PopulateSlotGame();
            _slotBehaviour.SocketConnected = true;
          }
          else
          {
            _uiManager.InitialiseUIData(initUIData.paylines);
          }
          break;
        }
      case "ResultData":
        {
          resultData = myData;
          isResultdone = true;
          break;
        }
      case "ExitUser":
        {
          // gameSocket.Disconnect();
          if (this.manager != null)
          {
            Debug.Log("Dispose my Socket");
            gameSocket.Disconnect();
            this.manager.Close();
          }
#if UNITY_WEBGL && !UNITY_EDITOR
          JSManager.SendCustomMessage("onExit");
#endif
          // exited = true;
          break;
        }
    }
  }

  private void PopulateSlotGame()
  {
    _slotBehaviour.SetInitialUI();
    _uiManager.InitialiseUIData(initUIData.paylines);
#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnEnter");
#endif
    RaycastBlocker.SetActive(false);
  }

  internal void AccumulateResult(int currBet)
  {
    isResultdone = false;
    MessageData message = new MessageData();
    message.type = "SPIN";
    message.payload.betIndex = currBet;

    // Serialize message data to JSON
    string json = JsonUtility.ToJson(message);
    SendDataWithNamespace("request", json);
  }
}


// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
[Serializable]
public class MessageData
{
  public string type;
  public Data payload = new();
}

[Serializable]
public class Data
{
  public int betIndex;
  public string Event;
  public List<int> index;
  public int option;
}

[Serializable]
public class DiamondMultiplier
{
  public List<int> range { get; set; }
  public int multiplier { get; set; }
}

[Serializable]
public class GameData
{
  public List<List<int>> lines { get; set; }
  public List<double> bets { get; set; }
}

[Serializable]
public class Paylines
{
  public List<Symbol> symbols { get; set; }
}

[Serializable]
public class Player
{
  public double balance { get; set; }
}

[Serializable]
public class Features
{
  public FreeSpin freeSpin { get; set; }
}
[Serializable]
public class FreeSpin
{
  public bool isEnabled { get; set; }
  public int triggerCount { get; set; }
  public int freeSpinCount { get; set; }
  public int incrementCount { get; set; }
  public List<DiamondMultiplier> diamondMultiplier { get; set; }
}

[Serializable]
public class Root
{
  public string id { get; set; }
  public GameData gameData { get; set; }
  public Features features { get; set; }
  public UiData uiData { get; set; }
  public Player player { get; set; }
  public bool success { get; set; }
  public List<List<string>> matrix { get; set; }
  public Payload payload { get; set; }
  public int freeSpinCount { get; set; }
  public int diamondCount { get; set; }
  public int diamondMultiplier { get; set; }
  public bool isFreeSpinTriggered { get; set; }
  public double freeSpinAccBalance { get; set; }

}

[Serializable]
public class Symbol
{
  public int id { get; set; }
  public string name { get; set; }
  public List<int> multiplier { get; set; }
  public string description { get; set; }
}

[Serializable]
public class UiData
{
  public Paylines paylines { get; set; }
}

[Serializable]
public class AuthTokenData
{
  public string cookie;
  public string socketURL;
  public string nameSpace;
}

[Serializable]
public class SlotImage
{
  public List<Image> slotImages = new List<Image>(10);
}

[Serializable]
public class Payload
{
  public double winAmount { get; set; }
  public List<Win> wins { get; set; }
}

[Serializable]
public class Win
{
  public int line { get; set; }
  public List<int> positions { get; set; }
  public double amount { get; set; }
  public bool usedScatter { get; set; }
}


