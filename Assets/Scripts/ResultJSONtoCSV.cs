using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Best.SocketIO;
using Best.SocketIO.Events;
using System;
using System.IO;

public class ResultJSONtoCSV : MonoBehaviour
{
  string s;
  [SerializeField] private string Token = "5e7e9ee221ea4a29845fec20ede675c1";
  [SerializeField] private string SocketURI = "https://frnp4zmn-5000.inc1.devtunnels.ms/";
  [SerializeField] private string NameSpace = "playground";
  [SerializeField] private int CurrentBetID = 0;
  [SerializeField] private int SpinCount = 5;
  [SerializeField] private int WildSymbolId = 11;

  private SocketManager Manager;
  private Socket GameSocket;
  private Root ResultData = null;
  private Root InitialData = null;
  private bool IsResultDone = false;

  private void Start()
  {
    OpenSocket();
  }

  void OpenSocket()
  {
    SocketOptions options = new SocketOptions();
    options.ReconnectionAttempts = 6;
    options.ReconnectionDelay = TimeSpan.FromSeconds(10);
    options.Reconnection = true;
    options.ConnectWith = Best.SocketIO.Transports.TransportTypes.WebSocket;

    Func<SocketManager, Socket, object> authFunction = (manager, socket) =>
    {
      return new
      {
        token = Token,
      };
    };
    options.Auth = authFunction;

    Manager = new SocketManager(new Uri(SocketURI), options);

    GameSocket = Manager.GetSocket("/" + NameSpace);

    GameSocket.On<ConnectResponse>(SocketIOEventTypes.Connect, OnConnected);
    GameSocket.On<string>(SocketIOEventTypes.Disconnect, OnDisconnected);
    GameSocket.On<string>("game:init", Parse);
    GameSocket.On<string>("spin:result", Parse);
  }

  void Parse(string data)
  {
    Debug.Log("Received data: " + data);
    Root root = null;
    root = JsonConvert.DeserializeObject<Root>(data);
    if (root == null)
    {
      Debug.LogError("Failed to deserialize data: " + data);
      return;
    }
    ResultData = null;

    string id = root.id;
    switch (id)
    {
      case "initData":
        Debug.Log("Received initData");
        InitialData = root;
        StartCoroutine(StartStoringResult());
        break;
      case "ResultData":
        Debug.Log("Received ResultData");
        ResultData = root;
        IsResultDone = true;
        break;
    }
  }

  void OnConnected(ConnectResponse response)
  {
    Debug.Log("Connected to socket");
  }

  void OnDisconnected(string data)
  {
    Debug.Log("Disconnected from socket: " + data);
  }

  void Spin()
  {
    if (GameSocket != null && GameSocket.IsOpen)
    {
      GameSocket.Emit("spin:request", JsonUtility.ToJson(new MessageData { currentBet = CurrentBetID }));
    }
    else
    {
      Debug.LogError("Socket is not connected");
    }
  }

  IEnumerator StartStoringResult()
  {
    string filePath = Path.Combine(Application.streamingAssetsPath, "data.csv");

    List<string[]> rowData = new()
    {
      new string[] { "Spin", "Before Balance", "After Balance", "Bet", "Total Win", "Total Win Lines", "Wild Used", "Free Spin Count", "Jackpot Amount" },
      new string[] { "0", InitialData.player.balance.ToString(), "0", "0", "0", "0", "0", "0", "0" }
    };

    double beforeBalance = InitialData.player.balance;

    for (int i = 0; i < SpinCount; i++)
    {
      Spin();
      yield return new WaitUntil(() => IsResultDone);
      IsResultDone = false;

      bool wildUsed = false;

      if (ResultData.payload.wins.Count > 0)
      {
        List<Win> totalWinLines = ResultData.payload.wins;
        for (int k = 0; k < totalWinLines.Count; k++)
        {
          List<List<int>> lines = InitialData.gameData.lines;
          List<int> line = lines[totalWinLines[k].line];
          List<List<string>> ResultMatrix = ResultData.matrix;
          for (int j = 0; j < totalWinLines[k].positions.Count; j++)
          {
            if (int.Parse(ResultMatrix[line[j]][j]) == WildSymbolId)
            {
              wildUsed = true;
              break;
            }
          }

          if (wildUsed)
          {
            break;
          }
        }
      }

      string spin = (i + 1).ToString();
      string balanceBefore = beforeBalance.ToString();
      string afterBalance = ResultData.player.balance.ToString();
      string bet = InitialData.gameData.bets[CurrentBetID].ToString();
      string totalWin = ResultData.payload.winAmount.ToString();
      string totalWinLine = ResultData.payload.wins.Count.ToString();
      string wild = wildUsed ? true.ToString() : false.ToString();
      string freeSpinCount = ResultData.freeSpin.count.ToString();
      string jackpotAmount = ResultData.jackpot.amount.ToString();

      rowData.Add(new string[] { spin, balanceBefore, afterBalance, bet, totalWin, totalWinLine, wild, freeSpinCount, jackpotAmount });

      beforeBalance = ResultData.player.balance;

      yield return new WaitForSeconds(0.01f); // Wait for a second before the next spin
    }

    using StreamWriter writer = new(filePath);
    foreach (string[] row in rowData)
    {
      string line = string.Join(",", row);
      writer.WriteLine(line);
    }

    Debug.Log("Data saved to " + filePath);
  }
}
