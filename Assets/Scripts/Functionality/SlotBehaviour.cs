using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System;

public class SlotBehaviour : MonoBehaviour
{
  [Header("Sprites")]
  [SerializeField] private Sprite[] myImages;  //images taken initially

  [Header("Slot Images")]
  [SerializeField] private List<SlotImage> images;     //class to store total images
  [SerializeField] private List<SlotImage> Tempimages;     //class to store the result matrix
  [SerializeField] private List<BoxScript> TempBoxScripts;
  [SerializeField] private List<Sprite> Box_Sprites;

  [Header("Slots Transforms")]
  [SerializeField] private Transform[] Slot_Transform;

  [Header("Buttons")]
  [SerializeField] private Button SlotStart_Button;
  [SerializeField] private Button AutoSpin_Button;
  [SerializeField] private Button AutoSpinStop_Button;
  [SerializeField] private Button TotalBetPlus_Button;
  [SerializeField] private Button TotalBetMinus_Button;
  [SerializeField] private Button LineBetPlus_Button;
  [SerializeField] private Button LineBetMinus_Button;
  [SerializeField] private Button SkipWinAnimation_Button;
  [SerializeField] private Button BonusSkipWinAnimation_Button;
  [SerializeField] private Button Turbo_Button;
  [SerializeField] private Button StopSpin_Button;

  [Header("Sprites")]
  [SerializeField] private Sprite[] Bonus_Sprite;
  [SerializeField] private Sprite[] Cleopatra_Sprite;
  [SerializeField] private Sprite TurboToggleSprite;

  [Header("Miscellaneous UI")]
  [SerializeField] private TMP_Text Balance_text;
  [SerializeField] private TMP_Text TotalBet_text;
  [SerializeField] private TMP_Text LineBet_text;
  [SerializeField] private TMP_Text TotalWin_text;
  [SerializeField] private TMP_Text BigWin_Text;
  [SerializeField] private TMP_Text BonusWin_Text;
  [SerializeField] private TMP_Text FSnum_text;

  [Header("Managers")]
  [SerializeField] internal AudioController audioController;
  [SerializeField] private SocketIOManager SocketManager;
  [SerializeField] private PayoutCalculation PayCalculator;
  [SerializeField] private UIManager uiManager;
  [SerializeField] private BonusController _bonusManager;

  protected int Lines = 20;
  private List<Tweener> alltweens = new List<Tweener>();
  private List<ImageAnimation> TempList = new();  //stores the sprites whose animation is running at present 
  private Coroutine AutoSpinRoutine = null;
  private Coroutine FreeSpinRoutine = null;
  private Coroutine tweenroutine;
  private Coroutine BoxAnimRoutine = null;
  private Tween BalanceTween;
  private bool IsAutoSpin = false;
  private bool IsFreeSpin = false;
  private bool WinAnimationFin = true;
  private bool CheckSpinAudio = false;
  private int BetCounter = 0;
  private double currentBalance = 0;
  private double currentTotalBet = 0;
  private int numberOfSlots = 5;          //number of columns
  private bool StopSpinToggle;
  private float SpinDelay = 0.2f;
  private bool IsTurboOn;
  private bool WasAutoSpinOn;
  private bool IsStopTweening;
  internal bool CheckPopups = false;
  internal bool IsSpinning = false;

  private void Start()
  {
    IsAutoSpin = false;

    if (Turbo_Button) Turbo_Button.onClick.RemoveAllListeners();
    if (Turbo_Button) Turbo_Button.onClick.AddListener(TurboToggle);

    if (StopSpin_Button) StopSpin_Button.onClick.RemoveAllListeners();
    if (StopSpin_Button) StopSpin_Button.onClick.AddListener(() => { audioController.PlayButtonAudio(); StopSpinToggle = true; StopSpin_Button.gameObject.SetActive(false); });

    if (SlotStart_Button) SlotStart_Button.onClick.RemoveAllListeners();
    if (SlotStart_Button) SlotStart_Button.onClick.AddListener(delegate
    {
      uiManager.CanCloseMenu();
      StartSlots();
    });

    if (TotalBetPlus_Button) TotalBetPlus_Button.onClick.RemoveAllListeners();
    if (TotalBetPlus_Button) TotalBetPlus_Button.onClick.AddListener(delegate
    {
      uiManager.CanCloseMenu();
      ChangeBet(true);
    });

    if (TotalBetMinus_Button) TotalBetMinus_Button.onClick.RemoveAllListeners();
    if (TotalBetMinus_Button) TotalBetMinus_Button.onClick.AddListener(delegate
    {
      uiManager.CanCloseMenu();
      ChangeBet(false);
    });

    if (LineBetPlus_Button) LineBetPlus_Button.onClick.RemoveAllListeners();
    if (LineBetPlus_Button) LineBetPlus_Button.onClick.AddListener(delegate
    {
      uiManager.CanCloseMenu();
      ChangeBet(true);
    });

    if (LineBetMinus_Button) LineBetMinus_Button.onClick.RemoveAllListeners();
    if (LineBetMinus_Button) LineBetMinus_Button.onClick.AddListener(delegate
    {
      uiManager.CanCloseMenu();
      ChangeBet(false);
    });

    if (AutoSpin_Button) AutoSpin_Button.onClick.RemoveAllListeners();
    if (AutoSpin_Button) AutoSpin_Button.onClick.AddListener(delegate
    {
      uiManager.CanCloseMenu();
      AutoSpin();
    });

    if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.RemoveAllListeners();
    if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.AddListener(delegate
    {
      uiManager.CanCloseMenu();
      StopAutoSpin();
    });

    if (SkipWinAnimation_Button) SkipWinAnimation_Button.onClick.RemoveAllListeners();
    if (SkipWinAnimation_Button) SkipWinAnimation_Button.onClick.AddListener(delegate
    {
      uiManager.CanCloseMenu();
      StopGameAnimation();
    });

    if (BonusSkipWinAnimation_Button) BonusSkipWinAnimation_Button.onClick.RemoveAllListeners();
    if (BonusSkipWinAnimation_Button) BonusSkipWinAnimation_Button.onClick.AddListener(delegate
    {
      uiManager.CanCloseMenu();
      StopGameAnimation();
    });
  }

  void TurboToggle()
  {
    audioController.PlayButtonAudio();
    if (IsTurboOn)
    {
      IsTurboOn = false;
      Turbo_Button.GetComponent<ImageAnimation>().StopAnimation();
      Turbo_Button.image.sprite = TurboToggleSprite;
    }
    else
    {
      IsTurboOn = true;
      Turbo_Button.GetComponent<ImageAnimation>().StartAnimation();
    }
  }

  #region Autospin
  private void AutoSpin()
  {
    if (!IsAutoSpin)
    {
      IsAutoSpin = true;
      if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(true);
      if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);

      if (AutoSpinRoutine != null)
      {
        StopCoroutine(AutoSpinRoutine);
        AutoSpinRoutine = null;
      }
      AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());
    }
  }

  private void StopAutoSpin()
  {
    if (IsAutoSpin)
    {
      StartCoroutine(StopAutoSpinCoroutine());
    }
  }

  private IEnumerator AutoSpinCoroutine()
  {
    while (IsAutoSpin)
    {
      StartSlots(IsAutoSpin);
      yield return tweenroutine;
      yield return new WaitForSeconds(SpinDelay);
    }
  }

  private IEnumerator StopAutoSpinCoroutine()
  {
    if (AutoSpinStop_Button) AutoSpinStop_Button.interactable = false;
    yield return new WaitUntil(() => !IsSpinning);
    ToggleButtonGrp(true);
    if (AutoSpinRoutine != null || tweenroutine != null)
    {
      StopCoroutine(AutoSpinRoutine);
      StopCoroutine(tweenroutine);
      if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
      if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(true);
      AutoSpinStop_Button.interactable = true;
      tweenroutine = null;
      AutoSpinRoutine = null;
      IsAutoSpin = false;
      StopCoroutine(StopAutoSpinCoroutine());
    }
  }
  #endregion

  #region FreeSpin
  internal void FreeSpin(int spins)
  {
    if (!IsFreeSpin)
    {
      if (FSnum_text) FSnum_text.text = spins.ToString();
      IsFreeSpin = true;
      ToggleButtonGrp(false);

      if (FreeSpinRoutine != null)
      {
        StopCoroutine(FreeSpinRoutine);
        FreeSpinRoutine = null;
      }
      FreeSpinRoutine = StartCoroutine(FreeSpinCoroutine(spins));
    }
  }

  private IEnumerator FreeSpinCoroutine(int spinchances)
  {
    int i = 0;
    int j = spinchances;
    while (i < spinchances)
    {
      j -= 1;
      if (FSnum_text) FSnum_text.text = j.ToString();

      StartSlots(false, true);

      yield return tweenroutine;
      yield return new WaitForSeconds(SpinDelay);
      i++;
    }
    IsFreeSpin = false;
    yield return _bonusManager.BonusGameEndRoutine();
    if (WasAutoSpinOn)
    {
      AutoSpin();
    }
    else
    {
      ToggleButtonGrp(true);
    }
  }
  #endregion

  private void CompareBalance()
  {
    if (currentBalance < currentTotalBet)
    {
      uiManager.LowBalPopup();
      SlotStart_Button.interactable = true;
    }
  }

  private void ChangeBet(bool IncDec)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (IncDec)
    {
      BetCounter++;
      if (BetCounter >= SocketManager.InitialData.bets.Count)
      {
        BetCounter = 0; // Loop back to the first bet
      }
    }
    else
    {
      BetCounter--;
      if (BetCounter < 0)
      {
        BetCounter = SocketManager.InitialData.bets.Count - 1; // Loop to the last bet
      }
    }
    if (LineBet_text) LineBet_text.text = SocketManager.InitialData.bets[BetCounter].ToString();
    if (TotalBet_text) TotalBet_text.text = (SocketManager.InitialData.bets[BetCounter] * Lines).ToString();
    currentTotalBet = SocketManager.InitialData.bets[BetCounter] * Lines;
    // CompareBalance();
  }

  #region InitialFunctions
  internal void shuffleInitialMatrix(bool midTween = false)
  {
    if (IsStopTweening || StopSpinToggle)
    {
      return;
    }
    for (int i = 0; i < images.Count; i++)
    {
      for (int j = 0; j < images[i].slotImages.Count; j++)
      {
        int randomIndex = UnityEngine.Random.Range(0, 13);
        if (j >= 8 && j <= 10 && midTween)
        {
          continue;
        }
        images[i].slotImages[j].sprite = myImages[randomIndex];
      }
    }
  }

  internal void SetInitialUI()
  {
    BetCounter = 0;
    if (LineBet_text) LineBet_text.text = SocketManager.InitialData.bets[BetCounter].ToString();
    if (TotalBet_text) TotalBet_text.text = (SocketManager.InitialData.bets[BetCounter] * Lines).ToString();
    if (TotalWin_text) TotalWin_text.text = "0.000";
    if (Balance_text) Balance_text.text = SocketManager.PlayerData.balance.ToString("f3");
    currentBalance = SocketManager.PlayerData.balance;
    currentTotalBet = SocketManager.InitialData.bets[BetCounter] * Lines;
    CompareBalance();
    uiManager.InitialiseUIData(SocketManager.InitUiData.paylines);
  }
  #endregion

  private void OnApplicationFocus(bool focus)
  {
    audioController.CheckFocusFunction(focus, CheckSpinAudio);
  }

  //function to populate animation sprites accordingly
  private void PopulateAnimationSprites(ImageAnimation animScript, int val)
  {
    animScript.textureArray.Clear();
    animScript.textureArray.TrimExcess();
    switch (val)
    {
      case 11:
        for (int i = 0; i < Bonus_Sprite.Length; i++)
        {
          animScript.textureArray.Add(Bonus_Sprite[i]);
        }
        animScript.AnimationSpeed = 12f;
        break;

      case 12:
        for (int i = 0; i < Cleopatra_Sprite.Length; i++)
        {
          animScript.textureArray.Add(Cleopatra_Sprite[i]);
        }
        animScript.AnimationSpeed = 15f;
        break;
    }
  }

  #region SlotSpin
  //starts the spin process
  private void StartSlots(bool autoSpin = false, bool bonus = false)
  {
    if (audioController) audioController.PlaySpinButtonAudio();

    if (!autoSpin)
    {
      if (AutoSpinRoutine != null)
      {
        StopCoroutine(AutoSpinRoutine);
        StopCoroutine(tweenroutine);
        tweenroutine = null;
        AutoSpinRoutine = null;
      }
    }
    //WinningsAnim(false);
    if (SlotStart_Button) SlotStart_Button.interactable = false;

    StopGameAnimation();

    //PayCalculator.ResetLines();
    tweenroutine = StartCoroutine(TweenRoutine(bonus));

    if (TotalWin_text) TotalWin_text.text = "0.000";
  }

  //manage the Routine for spinning of the slots
  private IEnumerator TweenRoutine(bool bonus = false)
  {
    if (currentBalance < currentTotalBet && !IsFreeSpin) // Check if balance is sufficient to place the bet
    {
      CompareBalance();
      StopAutoSpin();
      yield return new WaitForSeconds(1);
      yield break;
    }

    CheckSpinAudio = true;
    IsSpinning = true;
    ToggleButtonGrp(false);
    if (!IsTurboOn && !IsFreeSpin && !IsAutoSpin)
    {
      StopSpin_Button.gameObject.SetActive(true);
    }
    for (int i = 0; i < numberOfSlots; i++) // Initialize tweening for slot animations
    {
      InitializeTweening(Slot_Transform[i], bonus && i % 2 != 0);
      if (!bonus) yield return new WaitForSeconds(0.1f);
    }

    if (!bonus) // Deduct balance if not a bonus
    {
      BalanceDeduction();
    }

    SocketManager.AccumulateResult(BetCounter);
    yield return new WaitUntil(() => SocketManager.IsResultDone);
    currentBalance = SocketManager.PlayerData.balance;

    for (int i = 0; i < 3; i++)
    {
      for (int j = 0; j < 5; j++)
      {
        int resultNum = int.Parse(SocketManager.ResultData.matrix[i][j]);
        // print("resultNum: "+resultNum); 
        // print("image loc: " +j + " " + i);
        Tempimages[j].slotImages[i].sprite = myImages[resultNum];
        PopulateAnimationSprites(Tempimages[j].slotImages[i].GetComponent<ImageAnimation>(), resultNum);
      }
    }

    if (IsTurboOn)
    {
      yield return new WaitForSeconds(0.1f);
    }
    else
    {
      for (int i = 0; i < 5; i++)
      {
        yield return new WaitForSeconds(0.1f);
        if (StopSpinToggle)
        {
          break;
        }
      }
      StopSpin_Button.gameObject.SetActive(false);
    }
    IsStopTweening = true;
    for (int i = 0; i < numberOfSlots; i++) // Stop tweening for each slot
    {
      yield return StopTweening(Slot_Transform[i], i, bonus && i % 2 != 0, StopSpinToggle);
    }
    StopSpinToggle = false;
    yield return alltweens[^1].WaitForCompletion();
    KillAllTweens();

    if (SocketManager.ResultData.payload.winAmount > 0)
    {
      SpinDelay = 1.2f;
    }
    else
    {
      SpinDelay = 0.5f;
    }

    CheckWinLines(SocketManager.ResultData.payload.wins, SocketManager.ResultData.jackpot.amount);

    if (SocketManager.ResultData.payload.winAmount > 0) WinningsTextAnimation(bonus); // Trigger winnings animation if applicable

    CheckPopups = true;

    if (SocketManager.ResultData.jackpot.amount > 0) // Check for jackpot or winnings popups
    {
      yield return PlaySpecialSymbolAnimation(12);
      uiManager.PopulateWin(4);
    }
    else if (!bonus)
    {
      CheckWinPopups();
    }
    else
    {
      CheckPopups = false;
    }

    if (SocketManager.ResultData.payload.winAmount <= 0 && SocketManager.ResultData.jackpot.amount <= 0 && !SocketManager.ResultData.freeSpin.isFreeSpin)
    {
      audioController.PlayWLAudio("lose");
    }

    yield return new WaitUntil(() => !CheckPopups);

    if ((IsFreeSpin || IsAutoSpin) && BoxAnimRoutine != null && !WinAnimationFin) // Waits for winning payline animation to finish when triggered bonus
    {
      yield return new WaitUntil(() => WinAnimationFin);
      StopGameAnimation();
    }

    if (SocketManager.ResultData.freeSpin.isFreeSpin)
    {
      if (BoxAnimRoutine != null && !WinAnimationFin)
      {
        yield return new WaitUntil(() => WinAnimationFin);
        StopGameAnimation();
      }

      yield return new WaitForSeconds(1f);

      yield return PlaySpecialSymbolAnimation(11);

      yield return new WaitForSeconds(1f);

      if (!IsFreeSpin)
      {
        _bonusManager.StartBonus(SocketManager.ResultData.freeSpin.count);
      }
      else
      {
        IsFreeSpin = false;
        yield return StartCoroutine(_bonusManager.BonusInBonus());
      }

      if (IsAutoSpin)
      {
        WasAutoSpinOn = true;
        IsSpinning = false;
        StopAutoSpin();
      }
    }
    if (!IsAutoSpin && !IsFreeSpin) // Reset spinning state and toggle buttons
    {
      ToggleButtonGrp(true);
      IsSpinning = false;
    }
    else
    {
      IsSpinning = false;
    }
  }
  #endregion

  IEnumerator PlaySpecialSymbolAnimation(int symbolId)
  {
    List<ImageAnimation> TempList = new List<ImageAnimation>();
    for (int i = 0; i < 3; i++)
    {
      for (int j = 0; j < 5; j++)
      {
        int resultNum = int.Parse(SocketManager.ResultData.matrix[i][j]);
        if (resultNum == symbolId)
        {
          ImageAnimation anim = Tempimages[j].slotImages[i].GetComponent<ImageAnimation>();
          if (anim.textureArray.Count > 0)
          {
            anim.doLoopAnimation = false;
            TempList.Add(anim);
            anim.StartAnimation();
          }
        }
      }
    }

    if (TempList.Count > 0)
    {
      ImageAnimation anim = TempList[^1];
      yield return new WaitUntil(() => anim.rendererDelegate.sprite == anim.textureArray[^1]);
      foreach (ImageAnimation tempAnim in TempList)
      {
        tempAnim.StopAnimation();
      }
    }
  }

  internal void CheckWinPopups()
  {
    if (SocketManager.ResultData.payload.winAmount >= currentTotalBet * 5 && SocketManager.ResultData.payload.winAmount < currentTotalBet * 10)
    {
      uiManager.PopulateWin(1);
    }
    else if (SocketManager.ResultData.payload.winAmount >= currentTotalBet * 10 && SocketManager.ResultData.payload.winAmount < currentTotalBet * 15)
    {
      uiManager.PopulateWin(2);
    }
    else if (SocketManager.ResultData.payload.winAmount >= currentTotalBet * 15)
    {
      uiManager.PopulateWin(3);
    }
    else
    {
      CheckPopups = false;
    }
  }

  private void WinningsTextAnimation(bool bonus = false)
  {
    double winAmt = 0;
    double currentWin = 0;

    double currentBal = 0;
    double Balance = 0;

    double BonusWinAmt = 0;
    double currentBonusWinnings = 0;

    if (bonus)
    {
      try
      {
        BonusWinAmt = double.Parse(SocketManager.ResultData.payload.winAmount.ToString("f3"));
        currentBonusWinnings = double.Parse(BonusWin_Text.text);
      }
      catch (Exception e)
      {
        Debug.Log("Error while conversion " + e.Message);
      }
    }
    try
    {
      winAmt = double.Parse(SocketManager.ResultData.payload.winAmount.ToString("f3"));
    }
    catch (Exception e)
    {
      Debug.Log("Error while conversion " + e.Message);
    }

    try
    {
      currentBal = double.Parse(Balance_text.text);
    }
    catch (Exception e)
    {
      Debug.Log("Error while conversion " + e.Message);
    }

    try
    {
      Balance = double.Parse(SocketManager.PlayerData.balance.ToString("f3"));
    }
    catch (Exception e)
    {
      Debug.Log("Error while conversion " + e.Message);
    }

    try
    {
      currentWin = double.Parse(TotalWin_text.text);
    }
    catch (Exception e)
    {
      Debug.Log("Error while conversion " + e.Message);
    }

    if (bonus)
    {
      double CurrTotal = BonusWinAmt + currentBonusWinnings;
      DOTween.To(() => currentBonusWinnings, (val) => currentBonusWinnings = val, CurrTotal, 0.8f).OnUpdate(() =>
      {
        if (BonusWin_Text) BonusWin_Text.text = currentBonusWinnings.ToString("f3");
      });

      double start = 0;
      DOTween.To(() => start, (val) => start = val, BonusWinAmt, 0.8f).OnUpdate(() =>
      {
        if (BigWin_Text) BigWin_Text.text = start.ToString("f3");
      });
    }
    else
    {
      DOTween.To(() => currentWin, (val) => currentWin = val, winAmt, 0.8f).OnUpdate(() =>
      {
        if (TotalWin_text) TotalWin_text.text = currentWin.ToString("f3");
        if (BigWin_Text) BigWin_Text.text = currentWin.ToString("f3");
      });
      BalanceTween?.Kill();
      DOTween.To(() => currentBal, (val) => currentBal = val, Balance, 0.8f).OnUpdate(() =>
      {
        if (Balance_text) Balance_text.text = currentBal.ToString("f3");
      });
    }
  }

  private void BalanceDeduction()
  {
    double bet = 0;
    double balance = 0;
    try
    {
      bet = double.Parse(TotalBet_text.text);
    }
    catch (Exception e)
    {
      Debug.Log("Error while conversion " + e.Message);
    }

    try
    {
      balance = double.Parse(Balance_text.text);
    }
    catch (Exception e)
    {
      Debug.Log("Error while conversion " + e.Message);
    }
    double initAmount = balance;

    balance = balance - bet;

    BalanceTween = DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.3f).OnUpdate(() =>
    {
      if (Balance_text) Balance_text.text = initAmount.ToString("f3");
    });
  }


  void CheckWinLines(List<Win> wins, double jackpot = 0)
  {
    if (wins.Count <= 0)
    {
      return;
    }

    if (jackpot > 0)
    {
      if (audioController) audioController.PlayWLAudio("megaWin");
      for (int i = 0; i < Tempimages.Count; i++)
      {
        for (int k = 0; k < Tempimages[i].slotImages.Count; k++)
        {
          StartGameAnimation(Tempimages[i].slotImages[k].gameObject, TempBoxScripts[i].boxScripts[k]);
        }
      }
    }

    if (audioController) audioController.PlayWLAudio("win");
    List<KeyValuePair<int, int>> coords = new();
    for (int j = 0; j < wins.Count; j++)
    {
      for (int k = 0; k < wins[j].positions.Count; k++)
      {
        int rowIndex = SocketManager.InitialData.lines[wins[j].line][k];
        int columnIndex = k;
        coords.Add(new KeyValuePair<int, int>(rowIndex, columnIndex));
      }
    }

    foreach (var coord in coords)
    {
      int rowIndex = coord.Key;
      int columnIndex = coord.Value;
      StartGameAnimation(Tempimages[columnIndex].slotImages[rowIndex].gameObject, TempBoxScripts[columnIndex].boxScripts[rowIndex]);
    }

    if (!SocketManager.ResultData.freeSpin.isFreeSpin)
    {
      if (SkipWinAnimation_Button) SkipWinAnimation_Button.gameObject.SetActive(true);
    }

    if (IsFreeSpin && !SocketManager.ResultData.freeSpin.isFreeSpin)
    {
      if (BonusSkipWinAnimation_Button) BonusSkipWinAnimation_Button.gameObject.SetActive(true);
    }

    BoxAnimRoutine = StartCoroutine(WinLineLoopRoutine(wins));

    CheckSpinAudio = false;
  }

  IEnumerator WinLineLoopRoutine(List<Win> wins)
  {
    WinAnimationFin = false;
    while (true)
    {
      for (int i = 0; i < wins.Count; i++)
      {
        PayCalculator.GeneratePayoutLinesBackend(wins[i].line);
        PayCalculator.DontDestroyLines.Add(wins[i].line);
        for (int s = 0; s < 5; s++)
        {
          if (TempBoxScripts[s].boxScripts[SocketManager.LineData[wins[i].line][s]].isAnim)
          {
            TempBoxScripts[s].boxScripts[SocketManager.LineData[wins[i].line][s]].SetBG(Box_Sprites[wins[i].line]);
          }
        }
        if (wins.Count < 2)
        {
          WinAnimationFin = true;
          yield return new WaitForSeconds(2f);
          yield break;
        }
        yield return new WaitForSeconds(2f);
        for (int s = 0; s < 5; s++)
        {
          if (TempBoxScripts[s].boxScripts[SocketManager.LineData[wins[i].line][s]].isAnim)
          {
            TempBoxScripts[s].boxScripts[SocketManager.LineData[wins[i].line][s]].ResetBG();
          }
        }
        PayCalculator.DontDestroyLines.Clear();
        PayCalculator.DontDestroyLines.TrimExcess();
        PayCalculator.ResetStaticLine();
      }
      for (int i = 0; i < wins.Count; i++)
      {
        PayCalculator.GeneratePayoutLinesBackend(wins[i].line);
        PayCalculator.DontDestroyLines.Add(wins[i].line);
      }
      yield return new WaitForSeconds(2f);
      PayCalculator.DontDestroyLines.Clear();
      PayCalculator.DontDestroyLines.TrimExcess();
      PayCalculator.ResetStaticLine();
      WinAnimationFin = true;
    }
  }

  internal void CallCloseSocket()
  {
    SocketManager.CloseSocket();
  }

  void ToggleButtonGrp(bool toggle)
  {
    if (SlotStart_Button) SlotStart_Button.interactable = toggle;
    if (AutoSpin_Button && !IsAutoSpin) AutoSpin_Button.interactable = toggle;
    if (BetCounter != 0)
    {
      if (LineBetMinus_Button) LineBetMinus_Button.interactable = toggle;
      if (TotalBetMinus_Button) TotalBetMinus_Button.interactable = toggle;
    }
    if (BetCounter < SocketManager.InitialData.bets.Count - 1)
    {
      if (LineBetPlus_Button) LineBetPlus_Button.interactable = toggle;
      if (TotalBetPlus_Button) TotalBetPlus_Button.interactable = toggle;
    }
  }

  //Start the icons animation
  private void StartGameAnimation(GameObject animObjects, BoxScripting boxscript)
  {
    ImageAnimation temp = animObjects.GetComponent<ImageAnimation>();
    if (temp.textureArray.Count > 0)
    {
      temp.StartAnimation();
      TempList.Add(temp);
    }
    boxscript.isAnim = true;
  }

  //Stop the icons animation
  internal void StopGameAnimation()
  {
    if (BoxAnimRoutine != null)
    {
      StopCoroutine(BoxAnimRoutine);
      BoxAnimRoutine = null;
      WinAnimationFin = true;
    }

    if (TempBoxScripts.Count > 0)
    {
      for (int i = 0; i < TempBoxScripts.Count; i++)
      {
        foreach (BoxScripting b in TempBoxScripts[i].boxScripts)
        {
          b.isAnim = false;
          b.ResetBG();
        }
      }
    }

    if (SkipWinAnimation_Button) SkipWinAnimation_Button.gameObject.SetActive(false);
    if (BonusSkipWinAnimation_Button) BonusSkipWinAnimation_Button.gameObject.SetActive(false);

    if (TempList.Count > 0)
    {
      for (int i = 0; i < TempList.Count; i++)
      {
        TempList[i].StopAnimation();
      }
      TempList.Clear();
      TempList.TrimExcess();
    }

    PayCalculator.DontDestroyLines.Clear();
    PayCalculator.DontDestroyLines.TrimExcess();
    PayCalculator.ResetStaticLine();
  }

  #region TweeningCode
  private void InitializeTweening(Transform slotTransform, bool bonus = false)
  {
    Tweener tweener = null;
    if (bonus)
    {
      float yPos = slotTransform.localPosition.y;
      slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, -2393.65991f);
      tweener = slotTransform.DOLocalMoveY(-312.249969f, .3f).SetLoops(-1, LoopType.Restart)
        .SetEase(Ease.Linear).SetDelay(0);
    }
    else
    {
      slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, -312.249969f);
      tweener = slotTransform.DOLocalMoveY(-2393.65991f, .3f).SetLoops(-1, LoopType.Restart)
        .SetEase(Ease.Linear).SetDelay(0);
    }
    tweener.Play();
    alltweens.Add(tweener);
  }

  private IEnumerator StopTweening(Transform slotTransform, int index, bool bonus, bool isStop = false)
  {
    if (!isStop)
    {
      bool IsRegister = false;
      yield return alltweens[index].OnStepComplete(delegate { IsRegister = true; });
      yield return new WaitUntil(() => IsRegister);
    }

    alltweens[index].Kill();
    if (bonus)
      slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, -2393.65991f);
    else
      slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, -312.249969f);
    alltweens[index] = slotTransform.DOLocalMoveY(-729.26001f, 0.5f).SetEase(Ease.OutBack, 2)
      .OnComplete(() => { shuffleInitialMatrix(true); });

    if (audioController) audioController.PlayWLAudio("spinStop");
    if (!isStop)
    {
      yield return alltweens[index].WaitForCompletion();
    }
  }


  private void KillAllTweens()
  {
    for (int i = 0; i < numberOfSlots; i++)
    {
      alltweens[i].Kill();
    }
    alltweens.Clear();
    IsStopTweening = false;
  }
  #endregion

}

[Serializable]
public class SlotImage
{
  public List<Image> slotImages = new List<Image>(10);
}

[Serializable]
public class BoxScript
{
  public List<BoxScripting> boxScripts = new List<BoxScripting>(10);
}
