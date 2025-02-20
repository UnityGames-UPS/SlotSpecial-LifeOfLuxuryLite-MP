using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

public class UIManager : MonoBehaviour
{
    [Header("Main Popus UI Object")]
    [SerializeField] private GameObject MainPopup_Object;

    [Header("Paytable Popup UI References")]
    [SerializeField] private GameObject[] Pages;
    [SerializeField] private Button Paytable_Button;
    [SerializeField] private GameObject PaytablePopup_Object;
    [SerializeField] private Button PaytableExit_Button;
    [SerializeField] private Button PaytableLeft_Button;
    [SerializeField] private Button PaytableRight_Button;
    [SerializeField] private TMP_Text[] SymbolsText;
    [SerializeField] private TMP_Text FreeSpin_Text;
    [SerializeField] private TMP_Text Scatter_Text;
    [SerializeField] private TMP_Text numberOfDiamonds_Text;
    [SerializeField] private TMP_Text multiplier_Text;

    [Header("Sound/Music UI References")]
    [SerializeField] private Button Sound_Button;
    [SerializeField] private Button Music_Button;
    [SerializeField] private Sprite SoundOff_Sprite;
    [SerializeField] private Sprite SoundOn_Sprite;
    [SerializeField] private Sprite MusicOff_Sprite;
    [SerializeField] private Sprite MusicOn_Sprite;

    [Header("Win Popup UI References")]
    [SerializeField] private Image BigWin_Image;
    [SerializeField] private Image HugeWin_Image;
    [SerializeField] private Image MegaWin_Image;
    [SerializeField] private Image DoublePay_Image;
    [SerializeField] private GameObject WinPopup_Object;
    [SerializeField] private Transform WinPopupParent;
    [SerializeField] private TMP_Text Win_Text;
    [SerializeField] private Button SkipWinAnimation;
    private Image Win_Image;

    [Header("FreeSpins Popup UI References")]
    [SerializeField] private GameObject FreeSpinPopup_Object;
    [SerializeField] private GameObject FSBoard;
    [SerializeField] private GameObject FSTopBar;
    [SerializeField] private GameObject FSWinningsBoard;
    [SerializeField] private TMP_Text Free_Text;
    [SerializeField] private TMP_Text FSTotalWinnnings_Text;
    [SerializeField] private TMP_Text multiplierNo_Text;
    [SerializeField] private GameObject[] TopDiamondImages;
    [SerializeField] private Transform DummyDiamondDestination_Transform;
    [SerializeField] private ImageAnimation DiamondImageAnimation;
    [SerializeField] private GameObject FSPayoutTitle_Text;
    [SerializeField] internal TMP_Text FSNoBoard_Text;

    [Header("Disconnection Popup UI References")]
    [SerializeField] private Button CloseDisconnect_Button;
    [SerializeField] private GameObject DisconnectPopup_Object;

    [Header("AnotherDevice Popup UI References")]
    [SerializeField] private Button CloseAD_Button;
    [SerializeField] private GameObject ADPopup_Object;

    [Header("LowBalance Popup UI References")]
    [SerializeField] private Button LBExit_Button;
    [SerializeField] private GameObject LBPopup_Object;

    [Header("Quit Popup UI References")]
    [SerializeField] private GameObject QuitPopup_Object;
    [SerializeField] private Button YesQuit_Button;
    [SerializeField] private Button NoQuit_Button;
    [SerializeField] private Button CrossQuit_Button;
    [SerializeField] private Button GameExit_Button;

    [Header("Managers")]
    [SerializeField] private AudioController _audioController;
    [SerializeField] private SlotBehaviour _slotBehaviour;
    [SerializeField] private SocketIOManager _socketManager;

    private bool isMusic = true;
    private bool isSound = true;
    private bool isExit = false;
    private Tween WinPopupTextTween;
    private Tween ClosePopupTween;
    private Tween WinTextScaleTween;
    private Tween WinImageScaleTween;

    internal int FreeSpins;
    private int paytablePageCounter;

    private void Start()
    {
        isMusic = true;
        isSound = true;

        if (_audioController) _audioController.ToggleMute(false);

        if (Paytable_Button) Paytable_Button.onClick.RemoveAllListeners();
        if (Paytable_Button) Paytable_Button.onClick.AddListener(delegate { OpenPaytable(); });

        if (PaytableExit_Button) PaytableExit_Button.onClick.RemoveAllListeners();
        if (PaytableExit_Button) PaytableExit_Button.onClick.AddListener(delegate { ClosePopup(PaytablePopup_Object); });

        if (PaytableLeft_Button) PaytableLeft_Button.onClick.RemoveAllListeners();
        if (PaytableLeft_Button) PaytableLeft_Button.onClick.AddListener(() => { SwitchPages(false); });

        if (PaytableRight_Button) PaytableRight_Button.onClick.RemoveAllListeners();
        if (PaytableRight_Button) PaytableRight_Button.onClick.AddListener(() => { SwitchPages(true); });

        if (GameExit_Button) GameExit_Button.onClick.RemoveAllListeners();
        if (GameExit_Button) GameExit_Button.onClick.AddListener(delegate
        {
            OpenPopup(QuitPopup_Object);
        });

        if (NoQuit_Button) NoQuit_Button.onClick.RemoveAllListeners();
        if (NoQuit_Button) NoQuit_Button.onClick.AddListener(delegate
        {
            if (!isExit)
            {
                ClosePopup(QuitPopup_Object);
            }
        });

        if (CrossQuit_Button) CrossQuit_Button.onClick.RemoveAllListeners();
        if (CrossQuit_Button) CrossQuit_Button.onClick.AddListener(delegate
        {
            if (!isExit)
            {
                ClosePopup(QuitPopup_Object);
            }
        });

        if (LBExit_Button) LBExit_Button.onClick.RemoveAllListeners();
        if (LBExit_Button) LBExit_Button.onClick.AddListener(delegate { ClosePopup(LBPopup_Object); });

        if (YesQuit_Button) YesQuit_Button.onClick.RemoveAllListeners();
        if (YesQuit_Button) YesQuit_Button.onClick.AddListener(delegate
        {
            CallOnExitFunction();
        });

        if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.RemoveAllListeners();
        if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.AddListener(CallOnExitFunction);

        if (CloseAD_Button) CloseAD_Button.onClick.RemoveAllListeners();
        if (CloseAD_Button) CloseAD_Button.onClick.AddListener(CallOnExitFunction);

        if (Sound_Button) Sound_Button.onClick.RemoveAllListeners();
        if (Sound_Button) Sound_Button.onClick.AddListener(ToggleSound);

        if (Music_Button) Music_Button.onClick.RemoveAllListeners();
        if (Music_Button) Music_Button.onClick.AddListener(ToggleMusic);

        if (SkipWinAnimation) SkipWinAnimation.onClick.RemoveAllListeners();
        if (SkipWinAnimation) SkipWinAnimation.onClick.AddListener(SkipWin);
    }

    internal IEnumerator DiamondAnimation(Vector3 StartPosi, int DiamondCount, int multiplier){
        Vector3 midPosi=DiamondImageAnimation.transform.position;
        DiamondImageAnimation.transform.position = StartPosi;
        DiamondImageAnimation.transform.localScale = Vector3.one;
        _audioController.PlayDiamondAudio();
        DiamondImageAnimation.StartAnimation();
        yield return DiamondImageAnimation.transform.DOMove(midPosi, 1f).WaitForCompletion();
        yield return new WaitForSeconds(0.2f);
        DiamondImageAnimation.transform.DOScale(0, 1f);
        yield return DiamondImageAnimation.transform.DOMove(DummyDiamondDestination_Transform.position, 1f).WaitForCompletion();
        DiamondImageAnimation.StopAnimation();
        DiamondImageAnimation.transform.localScale = Vector3.zero;
        DiamondImageAnimation.transform.position=midPosi;
        foreach(GameObject go in TopDiamondImages){
            go.SetActive(false);
        }
        for(int i=0;i<TopDiamondImages.Length;i++){
            if(DiamondCount!=0){
                DiamondCount--;
                TopDiamondImages[i].SetActive(true);
            }
            else{
                break;
            }
        }
        multiplierNo_Text.text = multiplier.ToString();
    }

    private void OpenPaytable()
    {
        _audioController.PlayButtonAudio();
        foreach (GameObject gameObject in Pages)
        {
            gameObject.SetActive(false);
        }
        paytablePageCounter = 0;
        Pages[0].SetActive(true);
        MainPopup_Object.SetActive(true);
        PaytablePopup_Object.SetActive(true);
    }

    private void SwitchPages(bool IncDec)
    {
        _audioController.PlayButtonAudio();
        if (IncDec)
        {
            paytablePageCounter++;
            if (paytablePageCounter == Pages.Length)
            {
                paytablePageCounter = 0;
            }
            foreach (GameObject gameObject in Pages)
            {
                gameObject.SetActive(false);
            }
            Pages[paytablePageCounter].SetActive(true);
        }
        else
        {
            paytablePageCounter--;
            if (paytablePageCounter == -1)
            {
                paytablePageCounter = Pages.Length - 1;
            }
            foreach (GameObject gameObject in Pages)
            {
                gameObject.SetActive(false);
            }
            Pages[paytablePageCounter].SetActive(true);
        }
    }

    internal void LowBalPopup()
    {
        OpenPopup(LBPopup_Object);
    }

    internal void DisconnectionPopup()
    {
        if (!isExit)
        {
            isExit = true;
            OpenPopup(DisconnectPopup_Object);
        }
    }

    internal void PopulateWin(int value, double amount)
    {
        switch (value)
        {
            case 1:
                Win_Image = BigWin_Image;
                break;
            case 2:
                Win_Image = HugeWin_Image;
                break;
            case 3:
                Win_Image = MegaWin_Image;
                break;
            case 4:
                Win_Image = DoublePay_Image;
                break;
        }
        _audioController.PlayWLAudio("megaWin");
        StartPopupAnim(amount);
    }


    private void StartFreeSpins(int spins)
    {
        FreeSpinPopup_Object.transform.GetChild(0).transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).onComplete = () =>
        {
            FreeSpinPopup_Object.SetActive(false);
            _slotBehaviour.FreeSpin(spins);
        };
    }

    internal void FreeSpinProcess(int spins)
    {
        int ExtraSpins = spins - FreeSpins;
        FreeSpins = spins;

        if (Free_Text) Free_Text.text = ExtraSpins.ToString() + " Free spins awarded.";
        FreeSpinPopup_Object.transform.GetChild(0).transform.localScale = Vector3.zero;
        if (FreeSpinPopup_Object) FreeSpinPopup_Object.SetActive(true);
        FreeSpinPopup_Object.transform.GetChild(0).transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

        DOVirtual.DelayedCall(2f, () =>
        {
            StartFreeSpins(spins);
        });
    }

    internal void FreeSpinBoardToggle(bool toggle)
    {
        if (toggle)
        {
            foreach(GameObject i in TopDiamondImages){
                i.SetActive(false);
            }
            FSNoBoard_Text.text = "Free Spins: \n" + FreeSpins;
            FSBoard.SetActive(toggle);
            multiplierNo_Text.text = "1";
            FSTopBar.SetActive(toggle);
            FSTotalWinnnings_Text.text = "Total Win:\n0.000";
            FSWinningsBoard.SetActive(toggle);
        }
        else
        {
            FSBoard.SetActive(toggle);
            FSTopBar.SetActive(toggle);
            FSWinningsBoard.SetActive(toggle);
        }
    }

    void SkipWin()
    {
        Debug.Log("Skip win called");
        if (ClosePopupTween != null)
        {
            ClosePopupTween.Kill();
            ClosePopupTween = null;
        }
        if (WinPopupTextTween != null)
        {
            WinPopupTextTween.Kill();
            WinPopupTextTween = null;
            Win_Text.text = _socketManager.playerdata.currentWining.ToString("F3");
        }

        if (WinImageScaleTween != null)
        {
            WinImageScaleTween.Kill();
            WinImageScaleTween = null;
        }

        if (WinTextScaleTween != null)
        {
            WinTextScaleTween.Kill();
            WinTextScaleTween = null;
        }
        EndPopupAnim();
    }

    internal void OpenFSTotalWin(double amount){
        double initAmount=0;
        WinPopupParent.localScale = Vector3.zero;
        FSPayoutTitle_Text.SetActive(true);
        if (WinPopup_Object) WinPopup_Object.SetActive(true);
        WinImageScaleTween = WinPopupParent.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        WinPopupTextTween = DOTween.To(() => initAmount, (val) => initAmount = val, amount, 1.5f).OnUpdate(() =>
        {
            if (Win_Text) Win_Text.text = initAmount.ToString("F3");
        });
        ClosePopupTween = DOVirtual.DelayedCall(2f, () =>
        {
            SkipWin();
        });
    }

    private void StartPopupAnim(double amount)
    {
        double initAmount = 0;
        WinPopupParent.localScale = Vector3.zero;

        if (Win_Image) Win_Image.gameObject.SetActive(true);
        if (WinPopup_Object) WinPopup_Object.SetActive(true);
        WinImageScaleTween = WinPopupParent.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        WinPopupTextTween = DOTween.To(() => initAmount, (val) => initAmount = val, amount, 1.5f).OnUpdate(() =>
        {
            if (Win_Text) Win_Text.text = initAmount.ToString("F3");
        });

        ClosePopupTween = DOVirtual.DelayedCall(2f, () =>
        {
            SkipWin();
        });
    }

    void EndPopupAnim()
    {
        WinPopupParent.DOScale(Vector3.zero, 0.5f)
        .SetEase(Ease.InBack)
        .OnComplete(() =>
        {
            FSPayoutTitle_Text.SetActive(false);
            if (Win_Image) Win_Image.gameObject.SetActive(false);
            Win_Image = null;
            if (WinPopup_Object) WinPopup_Object.SetActive(false);
            _slotBehaviour._checkPopups = false;
        });
    }

    internal void ADfunction()
    {
        OpenPopup(ADPopup_Object);
    }

    internal void InitialiseUIData(Paylines symbolsText)
    {
        PopulateSymbolsPayout(symbolsText);
    }

    private void PopulateSymbolsPayout(Paylines paylines)
    {
        for (int i = 0; i < SymbolsText.Length; i++)
        {
            string text = null;
            if (paylines.symbols[i].Multiplier[0][0] != 0)
            {
                text += "5x - " + paylines.symbols[i].Multiplier[0][0] + "x";
            }
            if (paylines.symbols[i].Multiplier[1][0] != 0)
            {
                text += "\n4x - " + paylines.symbols[i].Multiplier[1][0] + "x";
            }
            if (paylines.symbols[i].Multiplier[2][0] != 0)
            {
                text += "\n3x - " + paylines.symbols[i].Multiplier[2][0] + "x";
            }
            if (SymbolsText[i]) SymbolsText[i].text = text;
        }

        for (int i = 0; i < paylines.symbols.Count; i++)
        {
            if (paylines.symbols[i].Name == "FreeSpin")
            {
                if (FreeSpin_Text) FreeSpin_Text.text = paylines.symbols[i].description.ToString();
            }
            if (paylines.symbols[i].Name == "Scatter")
            {
                if (Scatter_Text) Scatter_Text.text = paylines.symbols[i].description.ToString();
            }
        }

        for(int i=0;i<_socketManager.initialData.daimondMultipliers.Count;i++){
            DaimondMultiplier DM = _socketManager.initialData.daimondMultipliers[i];
            numberOfDiamonds_Text.text += DM.range[0].ToString()+"-"+DM.range[1].ToString();
            numberOfDiamonds_Text.text += "\n";
            multiplier_Text.text +=  DM.multiplier.ToString();
            multiplier_Text.text +=  "\n";
        }
    }

    private void CallOnExitFunction()
    {
        isExit = true;
        _audioController.PlayButtonAudio();
        _socketManager.CloseSocket();
    }

    private void OpenPopup(GameObject Popup)
    {
        if (_audioController) _audioController.PlayButtonAudio();
        if (Popup) Popup.SetActive(true);
        if (MainPopup_Object) MainPopup_Object.SetActive(true);
    }

    private void ClosePopup(GameObject Popup)
    {
        if (_audioController) _audioController.PlayButtonAudio();
        if (Popup) Popup.SetActive(false);
        if (!DisconnectPopup_Object.activeSelf)
        {
            if (MainPopup_Object) MainPopup_Object.SetActive(false);
        }
    }

    private void ToggleMusic()
    {
        _audioController.PlayButtonAudio();
        if (isMusic)
        {
            Music_Button.image.sprite = MusicOff_Sprite;
            _audioController.ToggleMute(true, "bg");
            isMusic = false;
        }
        else
        {
            Music_Button.image.sprite = MusicOn_Sprite;
            _audioController.ToggleMute(false, "bg");
            isMusic = true;
        }
    }

    private void ToggleSound()
    {
        _audioController.PlayButtonAudio();
        if (isSound)
        {
            Sound_Button.image.sprite = SoundOff_Sprite;
            if (_audioController) _audioController.ToggleMute(true, "button");
            if (_audioController) _audioController.ToggleMute(true, "wl");
            isSound = false;
        }
        else
        {
            Sound_Button.image.sprite = SoundOn_Sprite;
            if (_audioController) _audioController.ToggleMute(false, "button");
            if (_audioController) _audioController.ToggleMute(false, "wl");
            isSound = true;
        }
    }
}