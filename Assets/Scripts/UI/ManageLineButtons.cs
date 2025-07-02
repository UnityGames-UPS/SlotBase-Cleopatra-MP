using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ManageLineButtons : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{

  [SerializeField] private PayoutCalculation payManager;
  [SerializeField] private int num;
  private bool isEnabled = false;

  public void OnPointerEnter(PointerEventData eventData)
  {
    if (num <= 20)
    {
      isEnabled = true;
    }
    else
    {
      isEnabled = false;
    }
    if (isEnabled)
      payManager.GeneratePayoutLinesBackend(num - 1);
  }

  public void OnPointerExit(PointerEventData eventData)
  {
    if (isEnabled)
      payManager.ResetStaticLine();
  }

  public void OnPointerDown(PointerEventData eventData)
  {
    if (Application.platform == RuntimePlatform.WebGLPlayer && Application.isMobilePlatform)
    {
      //Debug.Log("run on pointer down");
      this.gameObject.GetComponent<Button>().Select();
      payManager.GeneratePayoutLinesBackend(num - 1);
    }
  }

  public void OnPointerUp(PointerEventData eventData)
  {
    if (Application.platform == RuntimePlatform.WebGLPlayer && Application.isMobilePlatform)
    {
      //Debug.Log("run on pointer up");
      payManager.ResetStaticLine();
      DOVirtual.DelayedCall(0.1f, () =>
      {
        this.gameObject.GetComponent<Button>().spriteState = default;
        EventSystem.current.SetSelectedGameObject(null);
      });
    }
  }
}
