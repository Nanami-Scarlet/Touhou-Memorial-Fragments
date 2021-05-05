using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioMgr : MonoSingleton<AudioMgr>, IInit
{
    private Dictionary<string, AudioClip> _dicStrClips = null;

    private AudioSource _bgmSource;
    private AudioSource _effSource;

    private Dictionary<AudioType, AudioSource> _dicTypeSource = new Dictionary<AudioType, AudioSource>(); 

    public void Init()
    {
        _dicStrClips = new Dictionary<string, AudioClip>();

        AudioClip[] clips = LoadMgr.Single.LoadAll<AudioClip>(Paths.AUDIO_FOLDER);
        foreach (AudioClip clip in clips)
        {
            _dicStrClips.Add(clip.name, clip);
        }

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _effSource = gameObject.AddComponent<AudioSource>();

        for(AudioType i = AudioType.Graze; i < AudioType.COUNT; ++i)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            AudioData data = DataMgr.Single.GetAudioData(i);

            source.clip = GetClip(data.Name);
            source.volume = data.Volume;

            _dicTypeSource.Add(i, source);
        }
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

    public void PlayBGM(string name, float vol = 1, bool loop = false)
    {
        AudioClip clip = GetClip(name);
        _bgmSource.loop = loop;
        _bgmSource.clip = clip;
        _bgmSource.volume = vol;

        _bgmSource.Play();
    }

    public void StopBGM()
    {
        _bgmSource.Pause();
    }

    public void ContinueBGM()
    {
        _bgmSource.UnPause();
    }

    public void PlayGameEff(AudioType type)
    {
        if (!_dicTypeSource.ContainsKey(type))
        {
            Debug.LogError("不存在的音频类型，类型为：" + type);
            return;
        }

        _dicTypeSource[type].Play();
    }

    public void StopGameEff(AudioType type)
    {
        if (!_dicTypeSource.ContainsKey(type))
        {
            Debug.LogError("不存在的音频类型，类型为：" + type);
            return;
        }

        _dicTypeSource[type].Stop();
    }

    public void PlayUIEff(string name, float volume = 1)
    {
        AudioClip clip = GetClip(name);

        _effSource.clip = clip;
        _effSource.volume = volume;
        _effSource.Play();
    }
}
