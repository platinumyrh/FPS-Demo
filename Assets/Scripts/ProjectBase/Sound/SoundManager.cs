using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SoundManager :BaseManager<SoundManager>
{
    private AudioSource bgmSource = null; //背景音乐的AudioSource组件
    private float BgmVolume = 1.0f; //背景音乐的音量


    private GameObject SFXObj = null; //音效的GameObject
    private List<AudioSource> sfxSources = new List<AudioSource>(); //音效的AudioSource组件列表

    public SoundManager()
    {
        MonoManager.GetInstance().AddUpdateListener(OnUpdate);
    }

    public void OnUpdate()
    {
        for (int i = sfxSources.Count - 1; i >= 0; i--)
        {
            if (!sfxSources[i].isPlaying)
            {
                GameObject.Destroy(sfxSources[i]);
                sfxSources.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 背景音乐相关
    /// </summary>
    /// <param name="name"></param>

    public void PlayBGM(string name)
    {
       // Debug.Log($"播放背景音乐: {name}");
        if (bgmSource == null)
        {
            GameObject bgmObject = new GameObject("BGMSource");
            bgmSource = bgmObject.AddComponent<AudioSource>();
        }
        ResManager.GetInstance().LoadAsync<AudioClip>(name, (clip) =>
        {
            bgmSource.clip = clip;
            bgmSource.volume = BgmVolume;
            bgmSource.loop = true;
            bgmSource.Play();
        });
    }

    public void StopBGM()
    {
    }

    public void PauseBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Pause();
        }
    }
    public void SetBGMVolume(float volume)
    {
        BgmVolume = volume;
        if (bgmSource != null)
        {
            bgmSource.volume = BgmVolume;
        }
    }

    ///summary>
    ///音效相关
    ///summary>
    public void PlaySFX(string name,UnityAction<AudioSource> callBack = null)
    {
        if (SFXObj == null)
        {
            SFXObj = new GameObject("SFXSource");
        }
        ResManager.GetInstance().LoadAsync<AudioClip>(name, (clip) =>
        {
            AudioSource source = SFXObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = 0.1f;
            source.Play();
            sfxSources.Add(source);
            if (callBack != null)
            {
                callBack.Invoke(source);
            }

        });
    }

    public void StopSFX(AudioSource source)
    {
        if (sfxSources.Contains(source))
        {
            source.Stop();
            sfxSources.Remove(source);
            GameObject.Destroy(source);
        }
    }
    public void SetSFXVolume(float volume)
    {
        foreach (var source in sfxSources)
        {
            source.volume = volume;
        }
    }


}
