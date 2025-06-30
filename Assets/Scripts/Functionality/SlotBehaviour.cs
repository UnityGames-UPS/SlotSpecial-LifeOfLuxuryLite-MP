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
  private int _betCounter = 0;
  private double _currentBalance = 0;
  private double _currentLineBet = 0;
  private double _currentTotalBet = 0;
  private int _lines = 15;
  private int _numberOfSlots = 5;          //number of columns
  private bool _stopSpinToggle;
  private float _spinDelay = 0.2f;
  private bool _isTurboOn;
  private bool _winningsAnimation = false;

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
      if (!_isFreeSpin && _socketManager.resultData.freeSpin != null && !_socketManager.resultData.freeSpin.isFreeSpin && _wasAutoSpinOn)
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
      i++;
    }
    _uiManager.FreeSpinBoardToggle(false);
    if (_socketManager.playerdata.currentWining > 0)
    {
      if (_totalWinText) _totalWinText.text = _socketManager.playerdata.currentWining.ToString("F3");
      _checkPopups = true;
      _uiManager.OpenFSTotalWin(_socketManager.playerdata.currentWining);
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
      if (_betCounter >= _socketManager.initialData.Bets.Count)
      {
        _betCounter = 0; // Loop back to the first bet
      }
    }
    else
    {
      _betCounter--;
      if (_betCounter < 0)
      {
        _betCounter = _socketManager.initialData.Bets.Count - 1; // Loop to the last bet
      }
    }
    if (_totalBetText) _totalBetText.text = (_socketManager.initialData.Bets[_betCounter] * _lines).ToString();
    _currentTotalBet = _socketManager.initialData.Bets[_betCounter] * _lines;
    _currentLineBet = _socketManager.initialData.Bets[_betCounter];
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
    _currentTotalBet = _socketManager.initialData.Bets[_betCounter] * _lines;
    _currentBalance = _socketManager.playerdata.Balance;
    _currentLineBet = _socketManager.initialData.Bets[_betCounter];

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
    _currentBalance = _socketManager.playerdata.Balance;

    for (int j = 0; j < _socketManager.resultData.resultSymbols.Count; j++)
    {
      for (int i = 0; i < _socketManager.resultData.resultSymbols[j].Count; i++)
      {
        _resultImages[i].slotImages[j].sprite = _symbolSprites[_socketManager.resultData.resultSymbols[j][i]];
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

    if (_socketManager.resultData.freeSpin.diamondCount != diamondCount && _isFreeSpin)
    {
      bool found = false;
      for (int i = 0; i < _resultImages.Count; i++)
      {
        for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
        {
          if (_socketManager.resultData.resultSymbols[j][i] == 8)
          {
            found = true;
            yield return _uiManager.DiamondAnimation(_resultImages[i].slotImages[j].transform.position, _socketManager.resultData.freeSpin.diamondCount, _socketManager.resultData.freeSpin.freeSpinMultiplier);
            break;
          }
        }
        if (found)
        {
          break;
        }
      }

      if (FSpayout != _socketManager.resultData.freeSpin.payout)
      {
        FSTotalWinnnings_Text.text = "Total Win\n" + _socketManager.resultData.freeSpin.payout.ToString("F3");
        _totalWinText.text = (_socketManager.resultData.freeSpin.payout - FSpayout).ToString("F3");
      }
      FSpayout = _socketManager.resultData.freeSpin.payout;
      diamondCount = _socketManager.resultData.freeSpin.diamondCount;
      freeSpinMultiplier = _socketManager.resultData.freeSpin.freeSpinMultiplier;
    }

    if (_socketManager.resultData.linesToEmit.Count > 0)
    {
      StartCoroutine(WinningLines(_socketManager.resultData.linesToEmit, _socketManager.resultData.symbolsToEmit));
    }
    else
    {
      _winningsAnimation = true;
    }

    yield return new WaitUntil(() => _winningsAnimation);

    if (_isAutoSpin || _isFreeSpin || _socketManager.resultData.freeSpin.isFreeSpin)
    {
      StopLoopCoroutine();
    }

    if (!_isFreeSpin)
    {
      CheckWinPopups();
      yield return new WaitUntil(() => !_checkPopups);
    }

    if (_socketManager.resultData.freeSpin.isFreeSpin)
    {
      _uiManager.FreeSpins = 0;
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
      freeSpinMultiplier = _socketManager.resultData.freeSpin.freeSpinMultiplier;
      diamondCount = _socketManager.resultData.freeSpin.diamondCount;
      FSpayout = _socketManager.resultData.freeSpin.payout;
      _uiManager.FreeSpinProcess((int)_socketManager.resultData.freeSpin.freeSpinCount);
    }

    if (_socketManager.playerdata.currentWining > 0)
    {
      _bottomBarText.text = "YOU WON: " + _socketManager.playerdata.currentWining.ToString("F3");
      _spinDelay = 1.2f;
    }
    else
    {
      _bottomBarText.text = "CLICK PLAY TO START!";
      _spinDelay = 0.2f;
    }
    if (_totalWinText) _totalWinText.text = _socketManager.playerdata.currentWining.ToString("F3");
    _balanceTween?.Kill();
    if (_balanceText) _balanceText.text = _socketManager.playerdata.Balance.ToString("F3");

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
    for (int i = 0; i < _socketManager.resultData.resultSymbols.Count; i++)
    {
      for (int j = 0; j < _socketManager.resultData.resultSymbols[i].Count; j++)
      {
        if (_socketManager.resultData.resultSymbols[i][j] == 9)
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
    if (_socketManager.resultData.doubleLines.Count > 0)
    {
      double payout = 0;
      foreach (IsDouble d in _socketManager.resultData.doubleLines)
      {
        payout += d.payout;
      }
      _uiManager.PopulateWin(4, payout);
    }
    else if (_socketManager.playerdata.currentWining >= _currentTotalBet * 5 && _socketManager.playerdata.currentWining < _currentTotalBet * 10)
    {
      _uiManager.PopulateWin(1, _socketManager.playerdata.currentWining);
    }
    else if (_socketManager.playerdata.currentWining >= _currentTotalBet * 10 && _socketManager.playerdata.currentWining < _currentTotalBet * 15)
    {
      _uiManager.PopulateWin(2, _socketManager.playerdata.currentWining);
    }
    else if (_socketManager.playerdata.currentWining >= _currentTotalBet * 15)
    {
      _uiManager.PopulateWin(3, _socketManager.playerdata.currentWining);
    }
    else
    {
      _checkPopups = false;
    }
  }

  void CheckWinAudio()
  {
    if (_socketManager.playerdata.currentWining > 0 && _socketManager.playerdata.currentWining < _currentTotalBet * 5 && _socketManager.resultData.doubleLines.Count == 0)
    {
      _audioController.PlayWLAudio("win");
    }
  }

  //Show winning lines
  private IEnumerator WinningLines(List<int> linesToEmit, List<List<int>> symbolsToEmit)
  {
    _winningsAnimation = false;
    ToggleSymbolsBlack(false);
    yield return new WaitForSeconds(0.5f);
    List<List<int>> uniqueList = GetUniqueList(symbolsToEmit);

    if (uniqueList.Count > 0)
    {
      foreach (List<int> pos in uniqueList)
      {
        for (int i = 0; i < pos.Count; i++)
        {
          Image boxImage = _resultImages[pos[0]].slotImages[pos[1]].transform.GetChild(0).GetComponent<Image>();
          boxImage.DOColor(_baseBlockColor, 0.2f);
          _resultImages[pos[0]].slotImages[pos[1]].DOColor(new Color(1f, 1f, 1f, 1f), 0.2f);
        }
      }

      foreach (int line in linesToEmit)
      {
        _paylineManager.GeneratePayLine(_winningLines[line - 1], _lineColors[line - 1]);
        _diamondImages[line - 1].DOFade(1f, 0.1f);
      }
      yield return new WaitForSeconds(2f);
      _winningsAnimation = true;
      _loopLinesCoroutine = StartCoroutine(LineLoop(linesToEmit));
    }
    else
    {
      Debug.LogError("unique list was empty");
    }
    _winningsAnimation = true;
  }

  private IEnumerator LineLoop(List<int> linesToEmit)
  {
    yield return new WaitForSeconds(2f);
    while (true)
    {
      foreach (int lineToEmit in linesToEmit)
      {
        ToggleSymbolsBlack(false);
        _paylineManager.ResetLines();

        yield return new WaitForSeconds(0.5f);

        List<int> line = _winningLines[lineToEmit - 1];
        for (int i = 0; i < line.Count; i++)
        {
          Image boxImage = _resultImages[i].slotImages[line[i]].transform.GetChild(0).GetComponent<Image>();
          boxImage.DOColor(_lineColors[lineToEmit - 1], 0.2f);
          _resultImages[i].slotImages[line[i]].DOColor(new Color(1f, 1f, 1f, 1f), 0.2f);
        }
        _diamondImages[lineToEmit - 1].DOFade(1f, 0.1f);
        _paylineManager.GeneratePayLine(line, _lineColors[lineToEmit - 1]);

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

  private List<List<int>> GetUniqueList(List<List<int>> symbolsToEmit)
  {
    HashSet<List<int>> uniqueSet = new HashSet<List<int>>();
    List<List<int>> uniqueList = new List<List<int>>();

    foreach (List<int> pos in symbolsToEmit)
    {
      uniqueSet.Add(pos);
    }

    foreach (List<int> pos in uniqueSet)
    {
      uniqueList.Add(pos);
    }

    return uniqueList;
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
