using System;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    public Sound[] sounds;
    [SerializeField] AudioMixer audioMixer;

    // Une liste des musique qui sont en cours de lecture
    // Faire pause à ces musiques si on est en pause

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        foreach(Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.outputAudioMixerGroup = s.mixer;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.spatialBlend = s.spatialBlend;
            s.source.playOnAwake = s.playOnAwake;
        }

    }

    public void Play(string name)
    {
        Sound s = Array.Find(sounds, sound=>sound.nameMusic == name);
        if (s == null)
        {
            return;
        }

        s.source.Play();
    }

    public void SetMainVolume(float volume)
    {
        audioMixer.SetFloat("mainVolume", MathF.Log10(volume) * 20f);
    }

    public void SetEffectVolume(float volume)
    {
        audioMixer.SetFloat("effectVolume", Mathf.Log10(volume) * 20f);
    }

    //private void Start()
    //{
    //    Play("JamSongFinal");
    //}
}
