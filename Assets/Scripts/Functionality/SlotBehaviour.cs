using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Color[] _boxColors;       //colors of the boxes
    [SerializeField] private Color _baseBlockColor;    //base color of the boxes

    [Header("Slot Images")]
    [SerializeField] private List<SlotImage> _totalImages;     //class to store total images
    [SerializeField] private List<SlotImage> _resultImages;     //class to store the result matrix

    [Header("Slots Transforms")]
    [SerializeField] private Transform[] _slotTransforms;

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

    [Header("Managers")]
    [SerializeField] private AudioController _audioController;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private SocketIOManager _socketManager;

    [Header("Auto spin setting")]

    internal bool _isAutoSpin = false;
    internal bool _checkPopups = false;

    private bool _wasAutoSpinOn;
    private List<Tween> _alltweens = new List<Tween>();
    private Coroutine _autoSpinRoutine = null;
    private Coroutine _freeSpinRoutine = null;
    private Coroutine _tweenRoutine;
    private Coroutine _loopComboCoroutine;
    private Tween _balanceTween;
    private List<Tween> _symbolTweens = new();
    private List<Tween> _boxColorTweens = new();
    private bool _isFreeSpin = false;
    private bool _isSpinning = false;
    private bool _checkSpinAudio = false;
    private int _betCounter = 0;
    private double _currentBalance = 0;
    private double _currentTotalBet = 0;
    private int _lines = 1;
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
            if(!_isFreeSpin && _socketManager.resultData.freeSpin!=null && !_socketManager.resultData.freeSpin.isFreeSpin && _wasAutoSpinOn){
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
        if (_wasAutoSpinOn){
            _wasAutoSpinOn=false;
            AutoSpin();
        }
        else{
            ToggleButtonGrp(true);
        }

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
    }

    #region InitialFunctions
    private void shuffleSlotImages(bool midTween = false)
    {
        for (int i = 0; i < _totalImages.Count; i++)
        {
            for (int j = 0; j < _totalImages[i].slotImages.Count; j++)
            {
                Sprite image = _symbolSprites[Random.Range(0, 10)];
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

        if (_totalBetText) _totalBetText.text = _currentTotalBet.ToString();
        if (_balanceText) _balanceText.text = _currentBalance.ToString("F3");
        if (_totalWinText) _totalWinText.text = "0.000";

        shuffleSlotImages();
        CompareBalance();
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

        if (_socketManager.resultData.winningCombinations.Count > 0)
        {
            StartCoroutine(WinningCombinations(_socketManager.resultData.winningCombinations));
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

        CheckWinPopups();
        yield return new WaitUntil(() => !_checkPopups);

        if (_socketManager.resultData.freeSpin.isFreeSpin)
        {
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
            _uiManager.FreeSpinProcess((int)_socketManager.resultData.freeSpin.freeSpinCount);
        }

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
        if (_socketManager.resultData.isDouble && _socketManager.playerdata.currentWining > 0)
        {
            _uiManager.PopulateWin(4, _socketManager.playerdata.currentWining);
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
        if (_socketManager.playerdata.currentWining > 0 && _socketManager.playerdata.currentWining < _currentTotalBet * 5 && !_socketManager.resultData.isDouble)
        {
            _audioController.PlayWLAudio("win");
        }
    }

    //Show winnings combination lines
    private IEnumerator WinningCombinations(List<WinningCombination> winningCombinations)
    {
        _winningsAnimation = false;
        yield return new WaitForSeconds(0.2f);
        ToggleSymbolsBlack(false);

        yield return new WaitForSeconds(0.2f);

        List<List<int>> uniqueList = GetUniqueList(winningCombinations);

        Tween resultImageTween = null;
        Tween boxColorTween = null;

        if (uniqueList.Count > 0)
        {
            foreach (List<int> pos in uniqueList)
            {
                for (int i = 0; i < pos.Count; i++)
                {
                    Image boxImage = _resultImages[pos[1]].slotImages[pos[0]].transform.GetChild(0).GetComponent<Image>();
                    boxImage.color = _baseBlockColor;
                    boxColorTween = boxImage.DOFade(1f, 0.5f).SetLoops(2, LoopType.Yoyo).OnComplete(() => boxColorTween.Kill());

                    resultImageTween = _resultImages[pos[1]].slotImages[pos[0]].DOColor(new Color(1f, 1f, 1f, 1f), 0.5f).SetLoops(2, LoopType.Yoyo).OnComplete(() => resultImageTween.Kill());
                }
            }
            yield return resultImageTween?.WaitForCompletion();
            yield return boxColorTween?.WaitForCompletion();
        }

        _winningsAnimation = true;
        _loopComboCoroutine = StartCoroutine(LoopCombos(winningCombinations));
    }

    private IEnumerator LoopCombos(List<WinningCombination> combos)
    {
        ToggleSymbolsBlack(false);
        while (true)
        {
            foreach (WinningCombination combo in combos)
            {
                foreach (List<int> pos in combo.positions)
                {
                    Image boxImage = _resultImages[pos[1]].slotImages[pos[0]].transform.GetChild(0).GetComponent<Image>();
                    boxImage.color = _boxColors[combo.symbolId];
                    Tween boxColorTween = boxImage.DOFade(1f, 0.5f).SetLoops(2, LoopType.Yoyo);

                    Tween symbolTween = _resultImages[pos[1]].slotImages[pos[0]].DOColor(new Color(1f, 1f, 1f, 1f), 0.5f).SetLoops(2, LoopType.Yoyo);

                    _symbolTweens.Add(symbolTween);
                    _boxColorTweens.Add(boxColorTween);
                }
                yield return new WaitForSeconds(1f);
                ToggleSymbolsBlack(false);
                ClearSymbolTweens();
            }
        }
    }

    private void ClearSymbolTweens()
    {
        if (_symbolTweens.Count > 0)
        {
            foreach (Tween tween in _symbolTweens)
            {
                tween.Kill();
            }
            _symbolTweens.Clear();
        }
        if (_boxColorTweens.Count > 0)
        {
            foreach (Tween tween in _boxColorTweens)
            {
                tween.Kill();
            }
            _boxColorTweens.Clear();
        }
    }

    private void StopLoopCoroutine()
    {
        if (_loopComboCoroutine != null)
            StopCoroutine(_loopComboCoroutine);
        ClearSymbolTweens();
        ToggleSymbolsBlack(true);
    }

    private void ToggleSymbolsBlack(bool toggle)
    {
        float value = toggle ? 1f : 0.3f;
        for (int i = 0; i < _resultImages.Count; i++)
        {
            for (int j = 0; j < _resultImages[i].slotImages.Count; j++)
            {
                Image boxImage = _resultImages[i].slotImages[j].transform.GetChild(0).GetComponent<Image>();
                boxImage.color = new Color(0f, 0f, 0f, 0f);
                _resultImages[i].slotImages[j].DOColor(new Color(value, value, value, 1f), 0.2f);
            }
        }
    }

    private List<List<int>> GetUniqueList(List<WinningCombination> combos)
    {
        HashSet<List<int>> uniqueSet = new HashSet<List<int>>();
        List<List<int>> uniqueList = new List<List<int>>();

        foreach (WinningCombination combo in combos)
        {
            foreach (List<int> pos in combo.positions)
            {
                uniqueSet.Add(pos);
            }
        }

        foreach (List<int> pos in uniqueSet)
        {
            uniqueList.Add(pos);
            // Debug.Log(pos);
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
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, -457f);
        Tween tween = slotTransform.DOLocalMoveY(-3116f, 0.2f).SetLoops(-1, LoopType.Restart);
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
        slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, -457f);
        _alltweens[index] = slotTransform.DOLocalMoveY(-840f, 0.5f).SetEase(Ease.OutElastic);
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