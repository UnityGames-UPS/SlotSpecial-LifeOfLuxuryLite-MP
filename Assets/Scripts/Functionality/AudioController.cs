using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField] private AudioSource bg_adudio;
    [SerializeField] internal AudioSource audioPlayer_wl;
    [SerializeField] internal AudioSource audioPlayer_button;
    [SerializeField] internal AudioSource audioSpin_button;
    [SerializeField] private AudioSource bonusBGAudioSource;
    [SerializeField] private AudioSource diamondSoundAudioSource;
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private AudioClip diamondAudioClip;

    private void Start()
    {
        if (bg_adudio) bg_adudio.Play();
        audioPlayer_button.clip = clips[clips.Length - 1];
        audioSpin_button.clip = clips[clips.Length - 2];
        diamondSoundAudioSource.clip = diamondAudioClip;
    }

    internal void CheckFocusFunction(bool focus, bool IsSpinning)
    {
        if (!focus)
        {
            bg_adudio.Pause();
            audioPlayer_wl.Pause();
            audioPlayer_button.Pause();
        }
        else
        {
            if (!bg_adudio.mute) bg_adudio.UnPause();
            if (IsSpinning)
            {
                if (!audioPlayer_wl.mute) audioPlayer_wl.UnPause();
            }
            else
            {
                StopWLAaudio();
            }
            if (!audioPlayer_button.mute) audioPlayer_button.UnPause();
        }
    }

    internal void PlayDiamondAudio(){
        diamondSoundAudioSource.loop=false;
        diamondSoundAudioSource.Play();
    }

    internal void SwitchBGSound(bool isbonus)
    {
        if(isbonus)
        {
            if (bonusBGAudioSource) bonusBGAudioSource.enabled = true;
            if (bg_adudio) bg_adudio.enabled = false;
        }
        else
        {
            if (bonusBGAudioSource) bonusBGAudioSource.enabled = false;
            if (bg_adudio) bg_adudio.enabled = true;
        }
    }

    internal void PlayWLAudio(string type)
    {
        audioPlayer_wl.loop = false;
        int index = 0;
        switch (type)
        {
            case "spin":
                index = 0;
                audioPlayer_wl.loop = true;
                break;
            case "win":
                index = 1;
                break;
            case "megaWin":
                index = 2;
                break;
        }
        StopWLAaudio();
        audioPlayer_wl.clip = clips[index];
        audioPlayer_wl.Play();
    }


    internal void PlayButtonAudio()
    {
        audioPlayer_button.Play();
    }

    internal void PlaySpinButtonAudio()
    {
        audioSpin_button.Play();
    }

    internal void StopWLAaudio()
    {
        audioPlayer_wl.Stop();
        audioPlayer_wl.loop = false;
    }

    internal void StopBgAudio()
    {
        bg_adudio.Stop();
    }

    internal void ToggleMute(bool toggle, string type = "all")
    {
        switch (type)
        {
            case "bg":
                bg_adudio.mute = toggle;
                bonusBGAudioSource.mute = toggle;
                break;
            case "button":
                audioPlayer_button.mute = toggle;
                audioSpin_button.mute = toggle;
                break;
            case "wl":
                audioPlayer_wl.mute = toggle;
                diamondSoundAudioSource.mute = toggle;
                break;
            case "all":
                audioPlayer_wl.mute = toggle;
                bg_adudio.mute = toggle;
                audioPlayer_button.mute = toggle;
                audioSpin_button.mute = toggle;
                break;
        }
    }

}
