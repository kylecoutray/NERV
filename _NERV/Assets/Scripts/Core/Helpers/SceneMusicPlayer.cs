using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SceneMusicPlayer : MonoBehaviour
{
    [Header("Drag your music clip here")]
    public AudioClip MusicClip;
    [Tooltip("Should the music loop?")]
    public bool Loop = true;

    AudioSource _audio;

    void Awake()
    {
        // grab/create AudioSource
        _audio = GetComponent<AudioSource>();
        _audio.clip        = MusicClip;
        _audio.loop        = Loop;
        _audio.playOnAwake = false;
    }

    void OnEnable()
    {
        if (MusicClip != null)
            _audio.Play();
    }

    void OnDisable()
    {
        if (_audio.isPlaying)
            _audio.Stop();
    }
}