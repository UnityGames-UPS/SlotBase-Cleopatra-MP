using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class InternetChecker : MonoBehaviour
{
  [SerializeField] private float checkInterval = 2f; // seconds
  private string Url = ""; // lightweight and usually available
  [SerializeField] private UIManager uIManager;
  [SerializeField] private SocketIOManager socketIOManager;

  void Start()
  {
#if UNITY_EDITOR
    Url = socketIOManager.TestSocketURI;
    StartCoroutine(CheckConnectionRoutine());
#elif UNITY_WEBGL
    StartCoroutine(WaitForURL());
#endif
  }

  IEnumerator WaitForURL()
  {
    while(string.IsNullOrEmpty(Url))
    {
      Url = socketIOManager.SocketURI; // Retry fetching the URL
      yield return null;
    }
 }

  IEnumerator CheckConnectionRoutine()
  {
    while (true)
    {
      yield return StartCoroutine(CheckInternetConnection((isConnected) =>
      {
        Debug.Log("Internet connected: " + isConnected);
        // You can trigger custom events here on disconnect/reconnect
        if (!isConnected)
        {
          uIManager.DisconnectionPopup();
        }
      }));

      yield return new WaitForSeconds(checkInterval);
    }
  }

  IEnumerator CheckInternetConnection(System.Action<bool> callback)
  {
    using (UnityWebRequest request = UnityWebRequest.Head(Url))
    {
      request.timeout = 5;
      yield return request.SendWebRequest();

      if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
      {
        callback(false);
      }
      else
      {
        callback(true);
      }
    }
  }
}
