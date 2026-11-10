using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простой аудио-менеджер: звуковые эффекты + фоновая музыка (тише эффектов).
/// Все клипы лежат в Assets/Resources/Audio и грузятся по имени файла.
/// </summary>
public class GameAudio : MonoBehaviour
{
    static GameAudio inst;
    AudioSource sfxSrc;
    AudioSource musicSrc;
    readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    string currentMusic = "";

    static GameAudio Get()
    {
        if (inst == null)
        {
            GameObject go = new GameObject("GameAudio");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<GameAudio>();

            inst.sfxSrc = go.AddComponent<AudioSource>();
            inst.sfxSrc.playOnAwake = false;
            inst.sfxSrc.spatialBlend = 0f;

            inst.musicSrc = go.AddComponent<AudioSource>();
            inst.musicSrc.playOnAwake = false;
            inst.musicSrc.loop = true;
            inst.musicSrc.volume = 0.28f;   // музыка тише эффектов
            inst.musicSrc.spatialBlend = 0f;
        }
        return inst;
    }

    public static AudioClip Load(string name)
    {
        GameAudio a = Get();
        AudioClip c;
        if (!a.cache.TryGetValue(name, out c))
        {
            c = Resources.Load<AudioClip>("Audio/" + name);
            a.cache[name] = c;
            if (c == null) Debug.LogWarning("GameAudio: нет клипа Resources/Audio/" + name);
        }
        return c;
    }

    public static void Play(string name, float volume = 1f)
    {
        Play(Load(name), volume);
    }

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        Get().sfxSrc.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    /// <summary>Клип из инспектора, а если он не назначен — клип по имени из Resources.</summary>
    public static void Play(AudioClip clip, string fallbackName, float volume = 1f)
    {
        Play(clip != null ? clip : Load(fallbackName), volume);
    }

    public static void PlayMusic(string name)
    {
        GameAudio a = Get();
        if (a.currentMusic == name && a.musicSrc.isPlaying) return;
        a.currentMusic = name;
        AudioClip c = Load(name);
        a.musicSrc.Stop();
        if (c == null) return;
        a.musicSrc.clip = c;
        a.musicSrc.Play();
    }

    /// <summary>На паузе делаем музыку тише (эффекты интерфейса остаются слышны).</summary>
    public static void DuckMusic(bool duck)
    {
        GameAudio a = Get();
        a.musicSrc.volume = duck ? 0.1f : 0.28f;
    }

    public static void StopMusic()
    {
        GameAudio a = Get();
        a.currentMusic = "";
        a.musicSrc.Stop();
    }
}
