using UnityEngine;
using UnityEngine.UI;

public class OrientationChange : MonoBehaviour
{
  [SerializeField] private RectTransform MainUITransform;

  void SwitchDisplay(string dimensions)
  {
    string[] parts = dimensions.Split(',');
    if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
    {
      Debug.Log($"Received Dimensions - Width: {width}, Height: {height}");
      if (width > height) //Landscape
      {
        MainUITransform.rotation = Quaternion.Euler(0, 0, 0);
      }
      else //Portrait
      {
        MainUITransform.rotation = Quaternion.Euler(0, 0, -90);
      }
      LayoutRebuilder.ForceRebuildLayoutImmediate(MainUITransform);
    }
    else
    {
      Debug.LogWarning("Invalid format received in SwitchDisplay");
    }
  }

  // private void Update()
  // {
  //   if (Input.GetKeyDown(KeyCode.Space))
  //   {
  //     SwitchDisplay(Screen.width + "," + Screen.height);
  //   }
  // }
}
