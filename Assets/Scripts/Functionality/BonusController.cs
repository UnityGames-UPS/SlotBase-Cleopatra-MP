using System.Collections;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class BonusController : MonoBehaviour
{
  [SerializeField] private SlotBehaviour slotManager;
  [SerializeField] private SocketIOManager SocketManager;
  [SerializeField] private AudioController _audioManager;
  [SerializeField] private ImageAnimation BonusOpen_ImageAnimation;
  [SerializeField] private ImageAnimation BonusClose_ImageAnimation;
  [SerializeField] private ImageAnimation BonusInBonus_ImageAnimation;
  [SerializeField] private GameObject BonusGame_Panel;
  [SerializeField] private GameObject BonusOpeningUI;
  [SerializeField] private GameObject BonusClosingUI;
  [SerializeField] private GameObject BonusInBonusUI;
  [SerializeField] private TMP_Text FSnum_Text;
  [SerializeField] private TMP_Text BonusOpeningText;
  [SerializeField] private TMP_Text BonusClosingText;
  [SerializeField] private TMP_Text BonusInBonusText;
  [SerializeField] private TMP_Text BonusWinningsText;
  [SerializeField] private RectTransform BonusOpeningTitleRT;
  [SerializeField] private RectTransform BonusInBonusTitleRT;
  [SerializeField] private RectTransform BonusClosingTitleRT;

  internal void StartBonus(int freespins)
  {
    if (FSnum_Text) FSnum_Text.text = freespins.ToString();
    if (BonusWinningsText) BonusWinningsText.text = "0.00";
    if (BonusGame_Panel) BonusGame_Panel.SetActive(true);
    StartCoroutine(BonusGameStartRoutine(freespins));
  }

  private IEnumerator BonusGameStartRoutine(int spins)
  {
    _audioManager.SwitchBGSound(true);
    if (BonusOpen_ImageAnimation) BonusOpen_ImageAnimation.StartAnimation();

    slotManager.StopGameAnimation();

    yield return new WaitUntil(() => BonusOpen_ImageAnimation.rendererDelegate.sprite == BonusOpen_ImageAnimation.textureArray[16]);

    BonusOpeningUI.SetActive(true);
    BonusOpen_ImageAnimation.PauseAnimation();
    yield return StartCoroutine(TextAnimation(BonusOpeningText, BonusOpeningTitleRT, spins, 0, true));
    BonusOpeningUI.SetActive(false);
    BonusOpen_ImageAnimation.ResumeAnimation();

    yield return new WaitUntil(() => BonusOpen_ImageAnimation.rendererDelegate.sprite == BonusOpen_ImageAnimation.textureArray[BonusOpen_ImageAnimation.textureArray.Count - 1]);
    BonusOpen_ImageAnimation.StopAnimation();

    yield return new WaitForSeconds(1f);

    slotManager.FreeSpin(spins);
  }

  internal IEnumerator BonusInBonus()
  {
    BonusInBonus_ImageAnimation.StartAnimation();

    yield return new WaitUntil(() => BonusInBonus_ImageAnimation.rendererDelegate.sprite == BonusInBonus_ImageAnimation.textureArray[5]);

    BonusInBonusUI.SetActive(true);
    BonusInBonus_ImageAnimation.PauseAnimation();

    if (!int.TryParse(FSnum_Text.text, out int currFS)) Debug.LogError("error while conversion");

    //FSnum_Text.text = SocketManager.resultData.freeSpins.count.ToString();

    yield return StartCoroutine(TextAnimation(BonusInBonusText, BonusInBonusTitleRT, SocketManager.ResultData.freeSpin.count - currFS, 0, true));
    BonusInBonusUI.SetActive(false);
    BonusInBonus_ImageAnimation.ResumeAnimation();

    yield return new WaitUntil(() => BonusInBonus_ImageAnimation.rendererDelegate.sprite == BonusInBonus_ImageAnimation.textureArray[BonusInBonus_ImageAnimation.textureArray.Count - 1]);
    BonusInBonus_ImageAnimation.StopAnimation();

    yield return new WaitForSeconds(1f);

    slotManager.FreeSpin(SocketManager.ResultData.freeSpin.count);
  }

  internal IEnumerator BonusGameEndRoutine()
  {
    BonusClose_ImageAnimation.StartAnimation();

    if (!double.TryParse(BonusWinningsText.text, out double totalWin))
    {
      Debug.LogError("error while conversion");
    }

    if (totalWin > 0)
    {
      yield return new WaitUntil(() => BonusClose_ImageAnimation.rendererDelegate.sprite == BonusClose_ImageAnimation.textureArray[6]);

      BonusClosingUI.SetActive(true);
      BonusClose_ImageAnimation.PauseAnimation();
      yield return StartCoroutine(TextAnimation(BonusClosingText, BonusClosingTitleRT, 0, totalWin));
      BonusClosingUI.SetActive(false);
      BonusClose_ImageAnimation.ResumeAnimation();
    }
    slotManager.StopGameAnimation();
    yield return new WaitUntil(() => BonusClose_ImageAnimation.rendererDelegate.sprite == BonusClose_ImageAnimation.textureArray[BonusClose_ImageAnimation.textureArray.Count - 1]);
    BonusClose_ImageAnimation.StopAnimation();
    _audioManager.SwitchBGSound(false);

    if (BonusGame_Panel) BonusGame_Panel.SetActive(false);
    BonusWinningsText.text = "0";
  }

  private IEnumerator TextAnimation(TMP_Text textObject, RectTransform imageObject, int IntGoal, double DoubleGoal, bool spin = false)
  {
    if (IntGoal != 0)
    {
      int start = 0;
      if (!spin)
      {
        DOTween.To(() => start, (val) => start = val, IntGoal, .8f).OnUpdate(() =>
        {
          if (textObject) textObject.text = start.ToString("f3");
        });
      }
      else
      {
        DOTween.To(() => start, (val) => start = val, IntGoal, .8f).OnUpdate(() =>
        {
          if (textObject) textObject.text = start.ToString() + " FREE SPINS.";
        });
      }

    }
    else if (DoubleGoal != 0)
    {
      double start = 0;
      DOTween.To(() => start, (val) => start = val, DoubleGoal, .8f).OnUpdate(() =>
       {
         if (textObject) textObject.text = start.ToString("f3");
       });
    }

    yield return imageObject.DOScale(new Vector2(1.5f, 1.5f), 1.5f).SetLoops(2, LoopType.Yoyo).SetDelay(0).WaitForCompletion();
    yield return imageObject.DOScale(new Vector2(1, 1), 0.5f).WaitForCompletion();
    yield return new WaitForSeconds(1f);
  }
}
