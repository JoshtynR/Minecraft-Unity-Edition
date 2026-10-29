using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }
    [Header("Menu Music")]
    public string menuSceneName = "MainMenu";
    [FormerlySerializedAs("tracks")]
    public List<AudioClip> menuTracks = new List<AudioClip>();
    [Header("World Music")]
    public string worldSceneName = "World";
    public List<AudioClip> worldTracks = new List<AudioClip>();
    [Header("Playback")]
    public bool playOnStart = true;
    public bool shuffle;
    public bool loopPlaylist = true;
    public bool loopTrack;
    [Header("Routing & Levels")]
    [Range(0f, 1f)] public float volume = 1f;
    public AudioMixerGroup output;
    [Header("Transitions")]
    [Tooltip("Seconds to crossfade before the current track ends. 0 = hard cut.")]
    [Min(0f)] public float crossfadeSeconds = 1.5f;
    [Header("Lifetime")]
    public bool dontDestroyOnLoad = true;

    private AudioSource _a, _b;
    private bool _usingA = true, _paused, _playbackRequested;
    private int _currentIndex = -1;
    private Coroutine _fadeCo;
    private System.Random _rng;
    private readonly List<int> _order = new List<int>();
    private readonly List<AudioClip> _snapshot = new List<AudioClip>();
    private bool _orderShuffled;
    private int _scenePool = -1;
    private List<AudioClip> tracks => _scenePool==0 ? menuTracks : _scenePool==1 ? worldTracks : null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
        _a = gameObject.AddComponent<AudioSource>(); _b = gameObject.AddComponent<AudioSource>();
        foreach (var source in new[] { _a, _b })
        {
            source.playOnAwake = false; source.loop = false; source.spatialBlend = 0; source.volume = 0;
            source.outputAudioMixerGroup = output;
        }
        _rng = new System.Random();
        _scenePool = ScenePool(SceneManager.GetActiveScene());
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }
    void Start()
    {
        _playbackRequested=playOnStart;
        SetVolume(PlayerPrefs.GetFloat("musicVolume",100f)/100f); EnsureOrder();
        if (playOnStart && _order.Count > 0) PlayIndex(_order[0], true);
    }
    void Update()
    {
        if (_a == null || !_playbackRequested || _paused || _fadeCo != null) return;
        var active = ActiveSource();
        if (AudioListener.pause && !active.ignoreListenerPause) return;
        if (active.clip == null) return;
        active.loop = loopTrack;
        if (loopTrack) { if (!active.isPlaying) active.Play(); return; }
        // Start exactly one transition. Never poll the finished outgoing source during a fade.
        if (!active.isPlaying || (crossfadeSeconds > 0 && active.clip.length - active.time <= crossfadeSeconds))
        {
            EnsureOrder(); int next = NextIndex();
            if (next >= 0) PlayIndex(next);
            else if (!active.isPlaying) StopAll(); // Let the final non-looping song finish.
        }
    }
    public void PlayIndex(int index, bool instant = false)
    {
        if (tracks == null || tracks.Count == 0) return;
        index = Mathf.Clamp(index, 0, tracks.Count - 1);
        if (tracks[index] == null) return;
        _currentIndex = index; BeginTrack(tracks[index], instant);
    }
    public void PlayClip(AudioClip clip, bool instant = false)
    {
        // Do not allow menu-only songs to leak into world playback or vice versa.
        if (clip == null || FindIndex(clip)<0) return;
        _currentIndex = FindIndex(clip); BeginTrack(clip, instant);
    }
    public void Play()
    {
        if (_paused && ActiveSource().clip != null)
        { _paused = false; _playbackRequested = true; _a.UnPause(); _b.UnPause(); }
        else if (!_playbackRequested || ActiveSource().clip == null)
        {
            EnsureOrder();
            if (_order.Count > 0) PlayIndex(_order.Contains(_currentIndex) ? _currentIndex : _order[0], true);
        }
    }
    public void Pause() { _paused = true; _a.Pause(); _b.Pause(); }
    public void StopAll()
    {
        CancelFade(); _paused = false; _playbackRequested = false;
        _a.Stop(); _b.Stop(); _a.clip = null; _b.clip = null; _a.volume = 0; _b.volume = 0;
    }
    public void Next()
    {
        EnsureOrder(); int next = NextIndex();
        if (next < 0) StopAll(); else PlayIndex(next);
    }
    public void Previous()
    {
        EnsureOrder(); if (_order.Count == 0) return;
        int previous = _order.IndexOf(_currentIndex) - 1;
        if (previous < 0) previous = loopPlaylist ? _order.Count - 1 : 0;
        PlayIndex(_order[previous]);
    }
    public void SetShuffle(bool enabled) { shuffle = enabled; }
    public void SetLoopPlaylist(bool enabled) { loopPlaylist = enabled; }
    public void SetLoopTrack(bool enabled)
    { loopTrack = enabled; if (_a != null) ActiveSource().loop = enabled; }
    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        if (_a != null && _fadeCo == null) { ActiveSource().volume = volume; InactiveSource().volume = 0; }
    }
    public int CurrentIndex => _currentIndex;
    public AudioClip CurrentClip => _a != null ? ActiveSource().clip : null;
    public bool IsPlaying => _a != null && ActiveSource().isPlaying;
    public void AddTrack(AudioClip clip)
    {
        if (clip == null) return;
        if (tracks != null) tracks.Add(clip);
    }
    public void RemoveTrackAt(int index)
    {
        if (tracks == null || index < 0 || index >= tracks.Count) return;
        if (index == _currentIndex) StopAll();
        tracks.RemoveAt(index); if (index <= _currentIndex) _currentIndex--;
    }
    private AudioSource ActiveSource() => _usingA ? _a : _b;
    private AudioSource InactiveSource() => _usingA ? _b : _a;
    private int FindIndex(AudioClip clip) => tracks == null ? -1 : tracks.IndexOf(clip);
    private void EnsureOrder()
    {
        bool changed = _orderShuffled != shuffle || _snapshot.Count != (tracks == null ? 0 : tracks.Count);
        if (!changed && tracks != null)
            for (int i = 0; i < tracks.Count; i++) if (_snapshot[i] != tracks[i]) { changed = true; break; }
        if (!changed) return;
        _snapshot.Clear(); _order.Clear(); _orderShuffled = shuffle;
        if (tracks == null) return;
        for (int i = 0; i < tracks.Count; i++) { _snapshot.Add(tracks[i]); if (tracks[i] != null) _order.Add(i); }
        if (shuffle)
        {
            ShuffleOrder();
            if (_order.Remove(_currentIndex)) _order.Insert(0, _currentIndex);
        }
    }
    private void ShuffleOrder()
    {
        for (int i = _order.Count - 1; i > 0; i--)
        { int j = _rng.Next(i + 1), value = _order[i]; _order[i] = _order[j]; _order[j] = value; }
    }
    private int NextIndex()
    {
        if (_order.Count == 0) return -1;
        int next = _order.IndexOf(_currentIndex) + 1;
        if (next < _order.Count) return _order[next];
        if (!loopPlaylist) return -1;
        if (shuffle)
        {
            ShuffleOrder();
            // Visit every valid entry once per pass; avoid an immediate repeat at the boundary.
            if (_order.Count > 1 && _order[0] == _currentIndex)
            { int value = _order[0]; _order[0] = _order[1]; _order[1] = value; }
        }
        return _order[0];
    }
    private void BeginTrack(AudioClip clip, bool instant)
    {
        bool canFade = !instant && !_paused && ActiveSource().isPlaying && crossfadeSeconds > 0;
        _paused = false; _playbackRequested = true;
        if (!canFade) { HardSwap(clip); return; }
        CancelFade(); var from = ActiveSource(); var to = InactiveSource();
        to.Stop(); to.clip = clip; to.time = 0; to.volume = 0; to.loop = loopTrack; to.Play();
        _usingA = !_usingA; // The incoming source is current, even while the old one fades out.
        float seconds = Mathf.Min(crossfadeSeconds, Mathf.Max(.01f, clip.length * .5f));
        _fadeCo = StartCoroutine(CrossfadeCo(from, to, seconds));
    }
    private void CancelFade() { if (_fadeCo != null) StopCoroutine(_fadeCo); _fadeCo = null; }
    private void HardSwap(AudioClip clip)
    {
        CancelFade(); _a.Stop(); _b.Stop(); _a.volume = 0; _b.volume = 0;
        var active = ActiveSource();
        active.clip = clip; active.time = 0; active.loop = loopTrack; active.volume = volume; active.Play();
    }
    private IEnumerator CrossfadeCo(AudioSource from, AudioSource to, float seconds)
    {
        float elapsed = 0, fromGain = volume > 0 ? from.volume / volume : 0;
        while (elapsed < seconds)
        {
            if (!_paused && (!AudioListener.pause || to.ignoreListenerPause))
            {
                elapsed += Time.unscaledDeltaTime; float k = Mathf.Clamp01(elapsed / seconds);
                to.volume = volume * k; from.volume = volume * fromGain * (1 - k);
            }
            yield return null;
        }
        from.Stop(); from.volume = 0; to.volume = volume; _fadeCo = null;
    }
    private int ScenePool(Scene scene)
    { return scene.name==menuSceneName ? 0 : scene.name==worldSceneName ? 1 : -1; }
    private void OnActiveSceneChanged(Scene previous, Scene next)
    {
        int pool=ScenePool(next);
        if(pool==_scenePool)return;
        bool resume=_playbackRequested,paused=_paused;
        // Stop both sources and any fade: outgoing scene music is exclusive.
        StopAll();_scenePool=pool;_currentIndex=-1;_snapshot.Clear();_order.Clear();
        EnsureOrder();
        if(resume&&_order.Count>0)
        {PlayIndex(_order[0],true);if(paused)Pause();}
        // Empty/unsupported scenes stay silent, retaining playback intent.
        _playbackRequested=resume;_paused=paused;
    }
    void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        if (Instance == this) Instance = null;
    }
}
