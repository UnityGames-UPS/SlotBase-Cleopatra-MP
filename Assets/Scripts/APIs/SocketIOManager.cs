using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;

public class SocketIOManager : MonoBehaviour
{
  [SerializeField] private SlotBehaviour SlotManager;
  [SerializeField] private UIManager UiManager;
  [SerializeField] internal JSFunctCalls JSManager;
  [SerializeField] internal string TestSocketURI = "https://frnp4zmn-5000.inc1.devtunnels.ms/";
  [SerializeField] private string TestToken;

  internal GameData InitialData = null;
  internal UiData InitUiData = null;
  internal Root ResultData = null;
  internal Player PlayerData = null;
  internal List<List<int>> LineData = null;
  internal bool IsResultDone = false;
  internal bool SetInit = false;

  internal string SocketURI = null;
  protected string NameSpace = "playground";
  protected string myAuth;
  protected string nameSpace;

  private SocketManager Manager;
  private Socket GameSocket;
  private const int maxReconnectionAttempts = 6;
  private readonly TimeSpan reconnectionDelay = TimeSpan.FromSeconds(10);

  private void Awake()
  {
    SetInit = false;
  }

  private void Start()
  {
    OpenSocket();
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
  void ReceiveAuthToken(string jsonData)
  {
    Debug.Log("Received data: " + jsonData);

    // Parse the JSON data
    var data = JsonUtility.FromJson<AuthTokenData>(jsonData);
    SocketURI = data.socketURL;
    myAuth = data.cookie;
    nameSpace = data.nameSpace;
    // Proceed with connecting to the server using myAuth and socketURL
  }

  private void OpenSocket()
  {
    //Create and setup SocketOptions
    SocketOptions options = new SocketOptions();
    options.ReconnectionAttempts = maxReconnectionAttempts;
    options.ReconnectionDelay = reconnectionDelay;
    options.Reconnection = true;
    options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket;

#if UNITY_WEBGL && !UNITY_EDITOR
        JSManager.SendCustomMessage("authToken");
        StartCoroutine(WaitForAuthToken(options));
#else
    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = TestToken,
      };
    };
    options.Auth = authFunction;
    SetupSocketManager(options);
#endif
    // #if UNITY_WEBGL && !UNITY_EDITOR
    //     string url = Application.absoluteURL;
    //     Debug.Log("Unity URL : " + url);
    //     ExtractUrlAndToken(url);

    //     Func<SocketManager, Socket, object> webAuthFunction = (manager, socket) =>
    //     {
    //       return new
    //       {
    //         token = TestToken,
    //       };
    //     };
    //     options.Auth = webAuthFunction;
    // #else
    //     Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    //     {
    //       return new
    //       {
    //         token = TestToken,
    //       };
    //     };
    //     options.Auth = authFunction;
    // #endif
    //     // Proceed with connecting to the server
    //     SetupSocketManager(options);
  }

  private void SetupSocketManager(SocketOptions options)
  {
    // Create and setup SocketManager
#if UNITY_EDITOR
    this.Manager = new SocketManager(new Uri(TestSocketURI), options);
#else
    this.Manager = new SocketManager(new Uri(SocketURI), options);
#endif

    if (string.IsNullOrEmpty(NameSpace) | string.IsNullOrWhiteSpace(NameSpace))
    {
      GameSocket = this.Manager.Socket;
    }
    else
    {
      Debug.Log("Namespace used :" + NameSpace);
      GameSocket = this.Manager.GetSocket("/" + NameSpace);
    }
    // Set subscriptions
    GameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
    GameSocket.On<string>(SocketIOEventTypes.Disconnect, OnDisconnected);
    GameSocket.On<string>(SocketIOEventTypes.Error, OnError);
    GameSocket.On<string>("game:init", OnListenEvent);
    GameSocket.On<string>("result", OnListenEvent);
    GameSocket.On<bool>("socketState", OnSocketState);
    GameSocket.On<string>("internalError", OnSocketError);
    GameSocket.On<string>("alert", OnSocketAlert);
    GameSocket.On<string>("AnotherDevice", OnSocketOtherDevice);
  }

  // Connected event handler implementation
  void OnConnected(ConnectResponse resp)
  {
    Debug.Log("Connected!");
    SendPing();
  }

  private void OnDisconnected(string response)
  {
    Debug.Log("Disconnected from the server");
    StopAllCoroutines();
    UiManager.DisconnectionPopup();
  }

  private void OnError(string response)
  {
    Debug.LogError("Error: " + response);
  }

  private void OnListenEvent(string data)
  {
    ParseResponse(data);
  }

  private void OnSocketState(bool state)
  {
    Debug.Log("my state is " + state);
  }

  private void OnSocketError(string data)
  {
    Debug.Log("Received error with data: " + data);
  }

  private void OnSocketAlert(string data)
  {
    Debug.Log("Received alert with data: " + data);
  }

  private void OnSocketOtherDevice(string data)
  {
    Debug.Log("Received Device Error with data: " + data);
    UiManager.ADfunction();
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
    if (GameSocket != null && GameSocket.IsOpen)
    {
      if (json != null)
      {
        GameSocket.Emit(eventName, json);
        Debug.Log("JSON data sent: " + json);
      }
      else
      {
        GameSocket.Emit(eventName);
      }
    }
    else
    {
      Debug.LogWarning("Socket is not connected.");
    }
  }

  internal void CloseSocket()
  {
    SendDataWithNamespace("game:exit");
#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnExit");
#endif
  }

  private void ParseResponse(string jsonObject)
  {
    Debug.Log(jsonObject);
    Root myData = new();
    myData = JsonConvert.DeserializeObject<Root>(jsonObject);

    string id = myData.id;

    switch (id)
    {
      case "initData":
        {
          InitialData = myData.gameData;
          InitUiData = myData.uiData;
          PlayerData = myData.player;
          LineData = myData.gameData.lines;
          if (!SetInit)
          {
            PopulateSlotSocket();
            SetInit = true;
          }
          else
          {
            RefreshUI();
          }
          break;
        }
      case "ResultData":
        {
          ResultData = myData;
          PlayerData = myData.player;
          IsResultDone = true;
          break;
        }
      case "ExitUser":
        {
          if (GameSocket != null)
          {
            Debug.Log("Dispose my Socket");
            GameSocket.Disconnect();
            this.Manager.Close();
          }
#if UNITY_WEBGL && !UNITY_EDITOR
          JSManager.SendCustomMessage("OnExit");
#endif
          break;
        }
    }
  }
  public void ExtractUrlAndToken(string fullUrl)
  {
    Uri uri = new Uri(fullUrl);
    string query = uri.Query; // Gets the query part, e.g., "?url=http://localhost:5000&token=e5ffa84216be4972a85fff1d266d36d0"

    Dictionary<string, string> queryParams = new Dictionary<string, string>();
    string[] pairs = query.TrimStart('?').Split('&');

    foreach (string pair in pairs)
    {
      string[] kv = pair.Split('=');
      if (kv.Length == 2)
      {
        queryParams[kv[0]] = Uri.UnescapeDataString(kv[1]);
      }
    }

    if (queryParams.TryGetValue("url", out string extractedUrl) &&
        queryParams.TryGetValue("token", out string token))
    {
      Debug.Log("Extracted URL: " + extractedUrl);
      Debug.Log("Extracted Token: " + token);
      TestToken = token;
      SocketURI = extractedUrl;
    }
    else
    {
      Debug.LogError("URL or token not found in query parameters.");
    }
  }
  private void RefreshUI()
  {
    UiManager.InitialiseUIData(InitUiData.paylines);
  }

  private void PopulateSlotSocket()
  {
    SlotManager.shuffleInitialMatrix();
    SlotManager.SetInitialUI();
#if UNITY_WEBGL && !UNITY_EDITOR
    JSManager.SendCustomMessage("OnEnter");
#endif
  }

  internal void AccumulateResult(int currBet)
  {
    IsResultDone = false;
    MessageData message = new MessageData();
    message.type = "SPIN";
    message.payload.betIndex = currBet;

    // Serialize message data to JSON
    string json = JsonUtility.ToJson(message);
    SendDataWithNamespace("request", json);
  }
}

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
public class GameData
{
  public List<List<int>> lines;
  public List<double> bets;
}

[Serializable]
public class Paylines
{
  public List<Symbol> symbols;
}

[Serializable]
public class Player
{
  public double balance;
}

[Serializable]
public class FreeSpins
{
  public int count { get; set; }
  public bool isFreeSpin { get; set; }
}

[SerializeField]
public class Bonus
{
  public int BonusSpinStopIndex { get; set; }
  public double amount { get; set; }
}

[Serializable]
public class Root
{
  //Result Data
  public bool success = false;
  public List<List<string>> matrix = new();
  public string name = "";
  public Payload payload = new();
  public Bonus bonus = new();
  public Jackpot jackpot = new();
  public Scatter scatter = new();
  public FreeSpins freeSpin = new();

  //Init Data
  public string id = "";
  public GameData gameData = new();
  public UiData uiData = new();
  public Player player = new();
}

[Serializable]
public class Scatter
{
  public double amount { get; set; }
}

[Serializable]
public class Jackpot
{
  public bool isTriggered { get; set; }
  public double amount { get; set; }
}

[Serializable]
public class Payload
{
  public double winAmount = 0.0;
  public List<Win> wins = new();
}

[Serializable]
public class Win
{
  public int line = 0;
  public List<int> positions = new();
  public double amount = 0.0;
}

[Serializable]
public class Symbol
{
  public int id;
  public string name;
  public List<int> multiplier;
  public string description;
}

[Serializable]
public class UiData
{
  public Paylines paylines;
}

[Serializable]
public class AuthTokenData
{
  public string cookie;
  public string socketURL;
  public string nameSpace;
}
