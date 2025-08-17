using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class MusicManager : MonoBehaviour
{
    // Optional singleton so you can call MusicManager.Instance.Next() from anywhere.
    public static MusicManager Instance { get; private set; }

    [Header("Playlist")]
    [Tooltip("Ordered list of music tracks to choose from.")]
    public List<AudioClip> tracks = new List<AudioClip>();

    [Header("Playback")]
    public bool playOnStart = true;
    public bool shuffle = false;
    public bool loopPlaylist = true;  // loops when reaching end
    public bool loopTrack = false;    // repeat the same song

    [Header("Routing & Levels")]
    [Range(0f, 1f)] public float volume = 1f;
    public AudioMixerGroup output;     // optional; leave null to use default

    [Header("Transitions")]
    [Tooltip("Seconds to crossfade between tracks. 0 = hard cut.")]
    [Min(0f)] public float crossfadeSeconds = 1.5f;

    [Header("Lifetime")]
    [Tooltip("Keep this music manager alive across scene loads.")]
    public bool dontDestroyOnLoad = true;

    // Internals
    private AudioSource _a, _b;     // double-source for crossfades
    private bool _usingA = true;
    private int _currentIndex = -1; // index in 'tracks'
    private Coroutine _fadeCo;
    private System.Random _rng;

    void Awake()
    {
        // Optional singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        // Make sure we have 2 audio sources for crossfading
        _a = gameObject.AddComponent<AudioSource>();
        _b = gameObject.AddComponent<AudioSource>();
        foreach (var src in new[] { _a, _b })
        {
            src.playOnAwake = false;
            src.loop = false; // we manage looping ourselves
            src.volume = 0f;
            if (output) src.outputAudioMixerGroup = output;
        }

        _rng = new System.Random();
    }

    void Start()
    {
        SetVolume(volume);

        if (playOnStart && tracks.Count > 0)
        {
            if (shuffle)
                PlayIndex(_rng.Next(tracks.Count), instant: true);
            else
                PlayIndex(0, instant: true);
        }
    }

    void Update()
    {
        // Auto-advance when the current source finishes
        var active = ActiveSource();
        if (!active.isPlaying && active.clip != null)
        {
            if (loopTrack)
            {
                PlayIndex(_currentIndex, instant: false); // re-trigger with crossfade
            }
            else
            {
                Next();
            }
        }
    }

    // ----- Public API -----

    public void PlayIndex(int index, bool instant = false)
    {
        if (tracks == null || tracks.Count == 0) return;
        index = Mathf.Clamp(index, 0, tracks.Count - 1);

        _currentIndex = index;
        var clip = tracks[_currentIndex];

        if (instant || crossfadeSeconds <= 0f)
            HardSwap(clip);
        else
            StartCrossfadeTo(clip);
    }

    public void PlayClip(AudioClip clip, bool instant = false)
    {
        if (clip == null) return;
        _currentIndex = FindIndex(clip);
        if (instant || crossfadeSeconds <= 0f)
            HardSwap(clip);
        else
            StartCrossfadeTo(clip);
    }

    public void Play()  // resume if paused, or start current index
    {
        var active = ActiveSource();
        if (active.clip != null)
        {
            active.UnPause();
            InactiveSource().Pause();
        }
        else if (tracks.Count > 0)
        {
            PlayIndex(Mathf.Max(0, _currentIndex), instant: true);
        }
    }

    public void Pause()
    {
        ActiveSource().Pause();
        InactiveSource().Pause();
    }

    public void StopAll()
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _a.Stop(); _b.Stop();
        _a.clip = null; _b.clip = null;
        _a.volume = 0f; _b.volume = 0f;
    }

    public void Next()
    {
        if (tracks.Count == 0) return;

        if (shuffle)
        {
            // Pick a track that isn't the current one if possible
            int next = (tracks.Count == 1) ? _currentIndex : _rng.Next(tracks.Count);
            if (tracks.Count > 1)
            {
                while (next == _currentIndex)
                    next = _rng.Next(tracks.Count);
            }
            PlayIndex(next);
        }
        else
        {
            int next = _currentIndex + 1;
            if (next >= tracks.Count)
            {
                if (!loopPlaylist) { StopAll(); return; }
                next = 0;
            }
            PlayIndex(next);
        }
    }

    public void Previous()
    {
        if (tracks.Count == 0) return;

        if (shuffle)
        {
            PlayIndex(_rng.Next(tracks.Count));
        }
        else
        {
            int prev = _currentIndex - 1;
            if (prev < 0) prev = loopPlaylist ? tracks.Count - 1 : 0;
            PlayIndex(prev);
        }
    }

    public void SetShuffle(bool enabled) => shuffle = enabled;
    public void SetLoopPlaylist(bool enabled) => loopPlaylist = enabled;
    public void SetLoopTrack(bool enabled) => loopTrack = enabled;

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        // Keep perceived loudness stable during crossfades: each source scales from 0..1
        // but cap each to 'volume'.
        var active = ActiveSource();
        var inactive = InactiveSource();
        if (_fadeCo == null)
        {
            active.volume = volume;
            inactive.volume = 0f;
        }
        // During fades, coroutine will drive volumes based on 'volume'
    }

    public int CurrentIndex => _currentIndex;
    public AudioClip CurrentClip => (_currentIndex >= 0 && _currentIndex < tracks.Count) ? tracks[_currentIndex] : null;
    public bool IsPlaying => ActiveSource().isPlaying;

    // Playlist management at runtime (optional)
    public void AddTrack(AudioClip clip) { if (clip != null) tracks.Add(clip); }
    public void RemoveTrackAt(int index)
    {
        if (index < 0 || index >= tracks.Count) return;
        bool removingCurrent = index == _currentIndex;
        tracks.RemoveAt(index);
        if (removingCurrent) StopAll();
        _currentIndex = Mathf.Clamp(_currentIndex, 0, Mathf.Max(0, tracks.Count - 1));
    }

    // ----- Internals -----

    private AudioSource ActiveSource() => _usingA ? _a : _b;
    private AudioSource InactiveSource() => _usingA ? _b : _a;

    private int FindIndex(AudioClip clip)
    {
        for (int i = 0; i < tracks.Count; i++)
            if (tracks[i] == clip) return i;
        return -1;
    }

    private void HardSwap(AudioClip clip)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);

        var active = ActiveSource();
        var inactive = InactiveSource();

        inactive.Stop();
        inactive.clip = clip;
        inactive.time = 0f;
        inactive.volume = volume;
        inactive.Play();

        active.Stop();
        active.volume = 0f;

        _usingA = !_usingA;
    }

    private void StartCrossfadeTo(AudioClip clip)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(CrossfadeCo(clip, crossfadeSeconds));
    }

    private IEnumerator CrossfadeCo(AudioClip newClip, float seconds)
    {
        var from = ActiveSource();
        var to = InactiveSource();

        // Prep 'to'
        to.Stop();
        to.clip = newClip;
        to.time = 0f;
        to.volume = 0f;
        to.Play();

        float t = 0f;
        seconds = Mathf.Max(0.01f, seconds);
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime; // unscaled so fades ignore timescale
            float k = Mathf.Clamp01(t / seconds);
            to.volume = volume * k;
            from.volume = volume * (1f - k);
            yield return null;
        }

        // Finish
        from.Stop();
        from.volume = 0f;
        to.volume = volume;
        _usingA = !_usingA;
        _fadeCo = null;
    }
}
