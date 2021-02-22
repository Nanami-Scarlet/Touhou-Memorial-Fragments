using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioMgr : MonoSingleton<AudioMgr>, IInit
{
    private Dictionary<string, AudioClip> _dicStrClips = null;

    private AudioSource _bgmSource;
    private AudioSource _effSource;

    public void Init()
    {
        _dicStrClips = new Dictionary<string, AudioClip>();

        AudioClip[] clips = LoadMgr.Single.LoadAll<AudioClip>(Paths.AUDIO_FOLDER);
        foreach(AudioClip clip in clips)
        {
            _dicStrClips.Add(clip.name, clip);
        }

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _effSource = gameObject.AddComponent<AudioSource>();
    }

    private AudioClip GetClip(string name)
    {
        if (!_dicStrClips.ContainsKey(name))
        {
            Debug.LogError("不存在该音频名，音频名为：" + name);
            return null;
        }

        return _dicStrClips[name];
    }

    public void PlayBGM(string name, bool loop = true)
    {
        AudioClip clip = GetClip(name);
        _bgmSource.loop = loop;
        _bgmSource.clip = clip;

        _bgmSource.Play();
    }

    public void PlayEff(string name, float vol = 1)
    {
        AudioClip clip = GetClip(name);

        _effSource.clip = clip;
        _effSource.volume = vol;
        _effSource.Play();
    }

    public void StopBGM()
    {
        _bgmSource.Pause();
    }

    public void ContinueBGM()
    {
        _bgmSource.UnPause();
    }
}
