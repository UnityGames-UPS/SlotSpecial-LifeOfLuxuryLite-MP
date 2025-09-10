using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class SlotBehaviour : MonoBehaviour
{
  [Header("Sprites")]
  [SerializeField] private Sprite[] _symbolSprites;  //images taken initially
  [SerializeField] Sprite _turboToggleSprite;

  [Header("Box Colors")]
  [SerializeField] private Color[] _lineColors;       //colors of the boxes
  [SerializeField] private Color _baseBlockColor;    //base color of the boxes

  [Header("Slot Images")]
  [SerializeField] private List<SlotImage> _totalImages;     //class to store total images
  [SerializeField] private List<SlotImage> _resultImages;     //class to store the result matrix

  [Header("Slots Transforms")]
  [SerializeField] private Transform[] _slotTransforms;

  [Header("UI Images")]
  [SerializeField] private Image[] _diamondImages;

  [Header("Buttons")]
  [SerializeField] private Button _spinButton;
  [SerializeField] private Button _stopSpinButton;
  [SerializeField] private Button _autoSpinButton;
  [SerializeField] private Button _autoSpinStopButton;
  [SerializeField] private Button _totalBetPlusButton;
  [SerializeField] private Button _totalBetMinusButton;
  [SerializeField] private Button _turboButton;

  [Header("UI Texts")]
  [SerializeField] private TMP_Text _balanceText;
  [SerializeField] private TMP_Text _totalBetText;
  [SerializeField] private TMP_Text _totalWinText;
  [SerializeField] private TMP_Text _bottomBarText;
  [SerializeField] private TMP_Text FSTotalWinnnings_Text;

  [Header("Managers")]
  [SerializeField] private AudioController _audioController;
  [SerializeField] private UIManager _uiManager;
  [SerializeField] private SocketIOManager _socketManager;
  [SerializeField] private PayLineManager _paylineManager;

  [Header("Auto spin setting")]

  internal bool SocketConnected = false;
  internal bool _isAutoSpin = false;
  internal bool _checkPopups = false;

  private int freeSpinMultiplier;
  private int diamondCount;
  private double FSpayout;
  private Dictionary<int, List<int>> _winningLines = new Dictionary<int, List<int>>();
  private bool _wasAutoSpinOn;
  private List<Tween> _alltweens = new List<Tween>();
  private Coroutine _autoSpinRoutine = null;
  private Coroutine _freeSpinRoutine = null;
  private Coroutine _tweenRoutine;
  private Coroutine _loopLinesCoroutine;
  private Tween _balanceTween;
  private bool _isFreeSpin = false;
  private bool _isSpinning = false;
  private bool _checkSpinAudio = false;
  internal int _betCounter = 0;
  private double _currentBalance = 0;
  private double _currentLineBet = 0;
  private double _currentTotalBet = 0;
  private int _lines = 15;
  private int _numberOfSlots = 5;          //number of columns
  private bool _stopSpinToggle;
  private float _spinDelay = 0.2f;
  private bool _isTurboOn;
  private bool _winningsAnimation = false;
  internal int freeSpinCount;
  internal double totalFSwin;

  private void Start()
  {
    _isAutoSpin = false;

    if (_spinButton) _spinButton.onClick.RemoveAllListeners();
    if (_spinButton) _spinButton.onClick.AddListener(delegate { StartSlots(); });

    if (_totalBetPlusButton) _totalBetPlusButton.onClick.RemoveAllListeners();
    if (_totalBetPlusButton) _totalBetPlusButton.onClick.AddListener(delegate { ChangeBet(true); });

    if (_totalBetMinusButton) _totalBetMinusButton.onClick.RemoveAllListeners();
    if (_totalBetMinusButton) _totalBetMinusButton.onClick.AddListener(delegate { ChangeBet(false); });

    if (_stopSpinButton) _stopSpinButton.onClick.RemoveAllListeners();
    if (_stopSpinButton) _stopSpinButton.onClick.AddListener(() => { _audioController.PlayButtonAudio(); _stopSpinToggle = true; _stopSpinButton.gameObject.SetActive(false); });

    if (_autoSpinButton) _autoSpinButton.onClick.RemoveAllListeners();
    if (_autoSpinButton) _autoSpinButton.onClick.AddListener(AutoSpin);

    if (_turboButton) _turboButton.onClick.RemoveAllListeners();
    if (_turboButton) _turboButton.onClick.AddListener(TurboToggle);

    if (_autoSpinStopButton) _autoSpinStopButton.onClick.RemoveAllListeners();
    if (_autoSpinStopButton) _autoSpinStopButton.onClick.AddListener(StopAutoSpin);
  }

  void PopulateLinesLocally()
  {
    int Count = 0;
    foreach (var line in _socketManager.initialData.lines)
    {
      _winningLines.Add(Count, line);
      Count++;
    }
  }

  internal void GenerateStaticLine(int val)
  {
    DestroyStaticLine();
    List<int> y_points = _winningLines[val];
    _paylineManager.GeneratePayLine(y_points, _lineColors[val], true);
  }

  internal void DestroyStaticLine()
  {
    _paylineManager.ResetStaticLine();
  }

  void TurboToggle()
  {
    _audioController.PlayButtonAudio();
    if (_isTurboOn)
    {
      _isTurboOn = false;
      _turboButton.GetComponent<ImageAnimation>().StopAnimation();
      _turboButton.image.sprite = _turboToggleSprite;
    }
    else
    {
      _isTurboOn = true;
      _turboButton.GetComponent<ImageAnimation>().StartAnimation();
    }
  }

  #region Autospin
  private void AutoSpin()
  {
    if (!_isAutoSpin)
    {
      _isAutoSpin = true;
      if (_autoSpinStopButton) _autoSpinStopButton.gameObject.SetActive(true);
      if (_autoSpinButton) _autoSpinButton.gameObject.SetActive(false);

      if (_autoSpinRoutine != null)
      {
        StopCoroutine(_autoSpinRoutine);
        _autoSpinRoutine = null;
      }
      _autoSpinRoutine = StartCoroutine(AutoSpinCoroutine());
    }
  }

  private void StopAutoSpin()
  {
    _audioController.PlayButtonAudio();
    if (_isAutoSpin)
    {
      _isAutoSpin = false;
      if (!_isFreeSpin && !_socketManager.resultData.isFreeSpinTriggered && _wasAutoSpinOn)
      {
        _wasAutoSpinOn = false;
      }
      if (_autoSpinStopButton) _autoSpinStopButton.gameObject.SetActive(false);
      if (_autoSpinButton) _autoSpinButton.gameObject.SetActive(true);
      StartCoroutine(StopAutoSpinCoroutine());
    }
  }

  private IEnumerator AutoSpinCoroutine()
  {
    while (_isAutoSpin)
    {
      StartSlots(_isAutoSpin);
      yield return _tweenRoutine;
      yield return new WaitForSeconds(_spinDelay);
    }
    if (_wasAutoSpinOn)
      _wasAutoSpinOn = false;
  }

  private IEnumerator StopAutoSpinCoroutine()
  {
    yield return new WaitUntil(() => !_isSpinning);
    ToggleButtonGrp(true);
    if (_autoSpinRoutine != null || _tweenRoutine != null)
    {
      StopCoroutine(_autoSpinRoutine);
      StopCoroutine(_tweenRoutine);
      _tweenRoutine = null;
      _autoSpinRoutine = null;
      StopCoroutine(StopAutoSpinCoroutine());
    }
  }
  #endregion

  #region FreeSpin
  internal void FreeSpin(int spins)
  {
    if (!_isFreeSpin)
    {
      _isFreeSpin = true;
      ToggleButtonGrp(false);

      if (_freeSpinRoutine != null)
      {
        StopCoroutine(_freeSpinRoutine);
        _freeSpinRoutine = null;
      }
      _freeSpinRoutine = StartCoroutine(FreeSpinCoroutine(spins));
    }
  }

  private IEnumerator FreeSpinCoroutine(int spinchances)
  {
    _audioController.SwitchBGSound(true);
    _uiManager.FreeSpinBoardToggle(true);
    int i = 0;
    while (i < spinchances)
    {
      _uiManager.FreeSpins--;
      _uiManager.FSNoBoard_Text.text = "Free Spins: \n" + _uiManager.FreeSpins.ToString();
      StartSlots();
      yield return _tweenRoutine;
      yield return new WaitForSeconds(_spinDelay);
      totalFSwin = _socketManager.resultData.freeSpinAccBalance;     //
      FSTotalWinnnings_Text.text = "Total Win:\n" + totalFSwin.ToString();
      i++;
    }
    _uiManager.FreeSpinBoardToggle(false);
    // if (_socketManager.resultData.payload.winAmount > 0)
    if (totalFSwin > 0)
    {
      if (_totalWinText) _totalWinText.text = _socketManager.resultData.payload.winAmount.ToString("F3");
      _checkPopups = true;
      // _uiManager.OpenFSTotalWin(_socketManager.resultData.payload.winAmount);
      _uiManager.OpenFSTotalWin(totalFSwin);
      yield return new WaitUntil(() => !_checkPopups);
    }
    if (_wasAutoSpinOn)
    {
      // yield return new WaitForSeconds(1f);
      _wasAutoSpinOn = false;
      AutoSpin();
    }
    else
    {
      ToggleButtonGrp(true);
    }
    _audioController.SwitchBGSound(false);
    _isFreeSpin = false;
  }
  #endregion

  private void CompareBalance()
  {
    if (_currentBalance < _currentTotalBet)
      _uiManager.LowBalPopup();
  }

  private void ChangeBet(bool IncDec)
  {
    if (_audioController) _audioController.PlayButtonAudio();
    if (IncDec)
    {
      _betCounter++;
      if (_betCounter >= _socketManager.initialData.bets.Count)
      {
        _betCounter = 0; // Loop back to the first bet
      }
    }
    else
    {
      _betCounter--;
      if (_betCounter < 0)
      {
        _betCounter = _socketManager.initialData.bets.Count - 1; // Loop to the last bet
      }
    }
    if (_totalBetText) _totalBetText.text = (_socketManager.initialData.bets[_betCounter] * _lines).ToString();
    _currentTotalBet = _socketManager.initialData.bets[_betCounter] * _lines;
    _currentLineBet = _socketManager.initialData.bets[_betCounter];
    populateLineBetOnDiamonds(_currentLineBet);

  }

  internal void populateLineBetOnDiamonds(double amt)
  {
    foreach (Image t in _diamondImages)
    {
      t.GetComponent<PaylineButton>().my_Text.text = amt.ToString();
    }
  }

  #region InitialFunctions
  private void shuffleSlotImages(bool midTween = false)
  {
    for (int i = 0; i < _totalImages.Count; i++)
    {
      for (int j = 0; j < _totalImages[i].slotImages.Count; j++)
      {
        Sprite image = _symbolSprites[UnityEngine.Random.Range(0, 10)];
        if (!midTween)
        {
          _totalImages[i].slotImages[j].sprite = image;
        }
        else
        {
          if (j == 10 || j == 11 || j == 12)
          {
            continue;
          }
          else
          {
            _totalImages[i].slotImages[j].sprite = image;
          }
        }
      }
    }
  }

  internal void SetInitialUI()
  {
    _betCounter = 0;
    _currentTotalBet = _socketManager.initialData.bets[_betCounter] * _lines;
    _currentBalance = _socketManager.playerdata.balance;
    _currentLineBet = _socketManager.initialData.bets[_betCounter];

    if (_totalBetText) _totalBetText.text = _currentTotalBet.ToString();
    if (_balanceText) _balanceText.text = _currentBalance.ToString("F3");
    if (_totalWinText) _totalWinText.text = "0.000";

    shuffleSlotImages();
    CompareBalance();
    PopulateLinesLocally();
    populateLineBetOnDiamonds(_currentLineBet);
  }
  #endregion

  private void OnApplicationFocus(bool focus)
  {
    _audioController.CheckFocusFunction(focus, _checkSpinAudio);
  }

  #region SlotSpin
  //starts the spin process
  private void StartSlots(bool autoSpin = false)
  {
    _totalWinText.text = "0.000";
    if (_spinButton) _spinButton.interactable = false;
    if (_audioController) _audioController.PlaySpinButtonAudio();

    if (!autoSpin)
    {
      if (_autoSpinRoutine != null)
      {
        StopCoroutine(_autoSpinRoutine);
        StopCoroutine(_tweenRoutine);
        _tweenRoutine = null;
        _autoSpinRoutine = null;
      }
    }
    StopLoopCoroutine();
    _tweenRoutine = StartCoroutine(TweenRoutine());
  }

  //manage the Routine for spinning of the slots
  private IEnumerator TweenRoutine()
  {
    if (_currentBalance < _currentTotalBet && !_isFreeSpin)
    {
      CompareBalance();
      StopAutoSpin();
      yield return new WaitForSeconds(1);
      ToggleButtonGrp(true);
      yield break;
    }

    if (_audioController) _audioController.PlayWLAudio("spin");

    _checkSpinAudio = true;
    _isSpinning = true;
    ToggleButtonGrp(false);

    if (!_isFreeSpin)
    {
      BalanceDeduction();
    }
    if (!_isTurboOn && !_isFreeSpin && !_isAutoSpin)
    {
      _stopSpinButton.gameObject.SetActive(true);
    }
    _bottomBarText.text = "GOOD LUCK!";
    for (int i = 0; i < _numberOfSlots; i++)
    {
      InitializeTweening(_slotTransforms[i]);
      yield return new WaitForSeconds(0.1f);
    }

    _socketManager.AccumulateResult(_betCounter);
    yield return new WaitUntil(() => _socketManager.isResultdone);
    _currentBalance = _socketManager.playerdata.balance;

    for (int j = 0; j < _socketManager.resultData.matrix.Count; j++)
    {
      for (int i = 0; i < _socketManager.resultData.matrix[j].Count; i++)
      {
        if (int.TryParse(_socketManager.resultData.matrix[j][i], out int symbolId))
        {
          _resultImages[i].slotImages[j].sprite = _symbolSprites[symbolId];
        }
      }
    }

    if (_isTurboOn)
    {
      _stopSpinToggle = true;
    }
    else
    {
      for (int i = 0; i < 5; i++)
      {
        yield return new WaitForSeconds(0.1f);
        if (_stopSpinToggle)
        {
          break;
        }
      }
      _stopSpinButton.gameObject.SetActive(false);
    }

    for (int i = 0; i < _numberOfSlots; i++)
    {
      yield return StopTweening(_slotTransforms[i], i, _stopSpinToggle);
    }
    _stopSpinToggle = false;
    if (_audioController) _audioController.StopWLAaudio();
    yield return _alltweens[^1].WaitForCompletion();
    KillAllTweens();
    shuffleSlotImages(true);

    if (_socketManager.resultData.diamondCount != diamondCount && _isFreeSpin)
    {
      bool found = false;
      for (int i = 0; i < _resultImages.Count; i++)
      {
        for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
        {
          if (int.TryParse(_socketManager.resultData.matrix[j][i], out int symbolId) && symbolId == 8)
          {
            found = true;
            yield return _uiManager.DiamondAnimation(_resultImages[i].slotImages[j].transform.position, _socketManager.resultData.diamondCount, _socketManager.resultData.diamondMultiplier);
            break;
          }
        }
        if (found)
        {
          break;
        }
      }

      if (FSpayout != _socketManager.resultData.payload.winAmount)
      {
        // FSTotalWinnnings_Text.text = "Total Win\n" + totalFSwin.ToString("F3");
        _totalWinText.text = (_socketManager.resultData.payload.winAmount - FSpayout).ToString("F3");
      }
      FSpayout = _socketManager.resultData.payload.winAmount;
      diamondCount = _socketManager.resultData.diamondCount;
      freeSpinMultiplier = _socketManager.resultData.diamondMultiplier;
    }

    if (_socketManager.resultData.payload.wins.Count > 0)
    {
      StartCoroutine(WinningLines(_socketManager.resultData.payload.wins));
    }
    else
    {
      _winningsAnimation = true;
    }

    yield return new WaitUntil(() => _winningsAnimation);

    if (_isAutoSpin || _isFreeSpin || _socketManager.resultData.isFreeSpinTriggered || _socketManager.resultData.freeSpinCount > 0)
    {
      StopLoopCoroutine();
    }


    if (!_isFreeSpin)
    {
      CheckWinPopups();
      yield return new WaitUntil(() => !_checkPopups);
    }

    if (_socketManager.resultData.isFreeSpinTriggered)
    {
      // _uiManager.FreeSpins += _socketManager.resultData.freeSpinCount;
      // _uiManager.FreeSpins = freeSpinCount;
      yield return FreeSpinSymbolLoop();
      if (_isAutoSpin)
      {
        _isAutoSpin = false;
        _wasAutoSpinOn = true;
        if (_autoSpinStopButton.gameObject.activeSelf)
        {
          _autoSpinStopButton.gameObject.SetActive(false);
          _autoSpinButton.interactable = false;
          _autoSpinButton.gameObject.SetActive(true);
        }
        StopCoroutine(_autoSpinRoutine);
        _autoSpinRoutine = null;
        yield return new WaitForSeconds(0.1f);
      }
      if (_isFreeSpin)
      {
        _isFreeSpin = false;
        if (_freeSpinRoutine != null)
        {
          StopCoroutine(_freeSpinRoutine);
          _freeSpinRoutine = null;
        }
      }
      freeSpinMultiplier = _socketManager.resultData.diamondMultiplier;    //////////////
      diamondCount = _socketManager.resultData.diamondCount;
      FSpayout = _socketManager.resultData.payload.winAmount;      ///////////
      _uiManager.FreeSpinProcess((int)_socketManager.resultData.freeSpinCount);
      // _uiManager.FreeSpinProcess(freeSpinCount);
    }

    if (_socketManager.resultData.payload.winAmount > 0)
    {
      _bottomBarText.text = "YOU WON: " + _socketManager.resultData.payload.winAmount.ToString("F3");
      _spinDelay = 1.2f;
    }
    else
    {
      _bottomBarText.text = "CLICK PLAY TO START!";
      _spinDelay = 0.2f;
    }
    if (_totalWinText) _totalWinText.text = _socketManager.resultData.payload.winAmount.ToString("F3");
    _balanceTween?.Kill();
    if (_balanceText) _balanceText.text = _socketManager.playerdata.balance.ToString("F3");

    if (!_isAutoSpin && !_isFreeSpin)
    {
      ToggleButtonGrp(true);
      _isSpinning = false;
    }
    else
    {
      _isSpinning = false;
    }
  }

  private IEnumerator FreeSpinSymbolLoop()
  {
    yield return new WaitForSeconds(0.2f);
    ToggleSymbolsBlack(false);
    yield return new WaitForSeconds(0.2f);
    Tween tempTween = null;
    for (int i = 0; i < _socketManager.resultData.matrix.Count; i++)
    {
      for (int j = 0; j < _socketManager.resultData.matrix[i].Count; j++)
      {
        if (int.Parse(_socketManager.resultData.matrix[i][j]) == 9)
        {
          Image boxImage = _resultImages[j].slotImages[i].transform.GetChild(0).GetComponent<Image>();
          boxImage.color = _baseBlockColor;
          _resultImages[j].slotImages[i].DOColor(new Color(1f, 1f, 1f, 1f), 0.5f).SetLoops(2, LoopType.Yoyo);
          tempTween = boxImage.DOFade(1f, 0.5f).SetLoops(2, LoopType.Yoyo);
        }
      }
    }
    yield return tempTween?.WaitForCompletion();
    ToggleSymbolsBlack(true);
    yield return new WaitForSeconds(0.2f);
  }

  private void BalanceDeduction()
  {
    if (!double.TryParse(_totalBetText.text, out double bet))
    {
      Debug.Log("Error while conversion");
    }
    if (!double.TryParse(_balanceText.text, out double balance))
    {
      Debug.Log("Error while conversion");
    }

    double initAmount = balance;
    balance -= bet;

    _balanceTween = DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.5f).OnUpdate(() =>
    {
      if (_balanceText) _balanceText.text = initAmount.ToString("F3");
    });
  }

  internal void CheckWinPopups()
  {
    _checkPopups = true;
    if (_socketManager.resultData.payload != null &&
        _socketManager.resultData.payload.wins != null &&
        _socketManager.resultData.payload.wins.Count > 0)
    {
      double payout = 0;
      foreach (var win in _socketManager.resultData.payload.wins)
      {
        payout += win.amount;
      }
      _uiManager.PopulateWin(4, payout);
    }
    else
    if (_socketManager.resultData.payload.winAmount >= _currentTotalBet * 5 && _socketManager.resultData.payload.winAmount < _currentTotalBet * 10)
    {
      _uiManager.PopulateWin(1, _socketManager.resultData.payload.winAmount);
    }
    else if (_socketManager.resultData.payload.winAmount >= _currentTotalBet * 10 && _socketManager.resultData.payload.winAmount < _currentTotalBet * 15)
    {
      _uiManager.PopulateWin(2, _socketManager.resultData.payload.winAmount);
    }
    else if (_socketManager.resultData.payload.winAmount >= _currentTotalBet * 15)
    {
      _uiManager.PopulateWin(3, _socketManager.resultData.payload.winAmount);
    }
    else
    {
      _checkPopups = false;
    }
  }

  void CheckWinAudio()
  {
    if (_socketManager.resultData.payload.winAmount > 0 && _socketManager.resultData.payload.winAmount < _currentTotalBet * 5 && _socketManager.resultData.gameData.lines.Count == 0)
    {
      _audioController.PlayWLAudio("win");
    }
  }

  //Show winning lines
  private IEnumerator WinningLines(List<Win> wins)
  {
    _winningsAnimation = false;
    ToggleSymbolsBlack(false);
    yield return new WaitForSeconds(0.5f);

    HashSet<(int row, int col)> uniquePositions = new();

    foreach (var win in wins)
    {
      foreach (int col in win.positions)
      {
        int row = _winningLines[win.line][col];
        uniquePositions.Add((row, col));
      }
    }

    if (uniquePositions.Count > 0)
    {
      foreach (var (row, col) in uniquePositions)
      {
        Image boxImage = _resultImages[col].slotImages[row].transform.GetChild(0).GetComponent<Image>();
        boxImage.DOColor(_baseBlockColor, 0.2f);
        _resultImages[col].slotImages[row].DOColor(new Color(1f, 1f, 1f, 1f), 0.2f);
      }

      foreach (var win in wins)
      {
        _paylineManager.GeneratePayLine(_winningLines[win.line], _lineColors[win.line]);
        _diamondImages[win.line].DOFade(1f, 0.1f);
      }

      yield return new WaitForSeconds(2f);

      _loopLinesCoroutine = StartCoroutine(LineLoop(wins));

    }
    else
    {
      Debug.LogError("No winning positions found.");
    }

    _winningsAnimation = true;
  }

  private IEnumerator LineLoop(List<Win> wins)
  {
    yield return new WaitForSeconds(2f);
    while (true)
    {
      foreach (var win in wins)
      {
        ToggleSymbolsBlack(false);
        _paylineManager.ResetLines();
        yield return new WaitForSeconds(0.5f);

        List<int> payline = _winningLines[win.line];

        foreach (int col in win.positions)
        {
          int row = payline[col];
          Image boxImage = _resultImages[col].slotImages[row].transform.GetChild(0).GetComponent<Image>();
          boxImage.DOColor(_lineColors[win.line], 0.2f);
          _resultImages[col].slotImages[row].DOColor(new Color(1f, 1f, 1f, 1f), 0.2f);
        }

        _diamondImages[win.line].DOFade(1f, 0.1f);
        _paylineManager.GeneratePayLine(payline, _lineColors[win.line]);

        yield return new WaitForSeconds(1f);
      }
    }
  }

  private void StopLoopCoroutine()
  {
    if (_loopLinesCoroutine != null)
      StopCoroutine(_loopLinesCoroutine);
    ToggleSymbolsBlack(true);
    _paylineManager.ResetLines();
  }

  private void ToggleSymbolsBlack(bool toggle)
  {
    float value = toggle ? 1f : 0.3f;
    for (int i = 0; i < _resultImages.Count; i++)
    {
      for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
      {
        Image boxImage = _resultImages[i].slotImages[j].transform.GetChild(0).GetComponent<Image>();
        boxImage.DOColor(new Color(0f, 0f, 0f, 0f), 0.2f);
        _resultImages[i].slotImages[j].DOColor(new Color(value, value, value, 1f), 0.2f);
      }
    }
    if (!toggle)
    {
      foreach (Image i in _diamondImages)
      {
        i.DOFade(0.1f, 0.2f);
      }
    }
    else
    {
      foreach (Image i in _diamondImages)
      {
        i.DOFade(1f, 0.2f);
      }
    }
  }

  #endregion

  void ToggleButtonGrp(bool toggle)
  {
    if (_spinButton) _spinButton.interactable = toggle;
    if (_autoSpinButton) _autoSpinButton.interactable = toggle;
    if (_totalBetMinusButton) _totalBetMinusButton.interactable = toggle;
    if (_totalBetPlusButton) _totalBetPlusButton.interactable = toggle;
  }

  #region TweeningCode
  private void InitializeTweening(Transform slotTransform)
  {
    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, -329f);
    Tween tween = slotTransform.DOLocalMoveY(-2960f, 0.2f).SetLoops(-1, LoopType.Restart);
    _alltweens.Add(tween);
  }

  private IEnumerator StopTweening(Transform slotTransform, int index, bool isStop)
  {
    if (!isStop)
    {
      bool isComplete = false;
      _alltweens[index].OnStepComplete(() => isComplete = true);
      yield return new WaitUntil(() => isComplete);
    }
    _alltweens[index].Kill();
    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, -329f);
    _alltweens[index] = slotTransform.DOLocalMoveY(-769.1f, 0.5f).SetEase(Ease.OutElastic);
    if (!isStop)
    {
      yield return new WaitForSeconds(0.2f);
    }
    else
    {
      yield return null;
    }
  }

  private void KillAllTweens()
  {
    if (_alltweens.Count > 0)
    {
      for (int i = 0; i < _alltweens.Count; i++)
      {
        _alltweens[i].Kill();
      }
      _alltweens.Clear();
    }
  }
  #endregion
}
