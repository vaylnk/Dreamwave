using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrumManager : MonoBehaviour
{
    public static StrumManager SM_Instance;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;

    [Header("Timing")]
    public float JudgementTimeMs { get; private set; }
    public float SongTimeMs { get; private set; }

    [Header("Scroll")]
    public float ScrollSpeed;
    public float strumLineY;
    [SerializeField] private float unitsPerSecond = 200f;
    public float _playerScrollMultiplier = 1f;

    [Header("Windows")]
    [SerializeField] private float visibleWindowMs = 5000f;
    [SerializeField] private float spawnLeadMs = 1500f;
    [SerializeField] private float despawnLagMs = 400f;
    [SerializeField] private float positionEpsilon = 0.01f;
    [SerializeField] private float physicsSimWindowMs = 500f;

    private double _songDspStart;
    private float _visualSongTime;
    private bool _initialized;

    // All chart notes, sorted by time.
    private MsNote[] _allNotes = Array.Empty<MsNote>();
    private Rigidbody2D[] _rigidbodies = Array.Empty<Rigidbody2D>();
    private Collider2D[] _colliders = Array.Empty<Collider2D>();
    private int _noteCount;

    // Spawn cursor for sorted notes.
    private int _nextSpawnIndex;

    // Backwards-compatible active list used by your existing systems.
    public List<MsNote> activeNotes = new List<MsNote>(256);

    private void Awake()
    {
        SM_Instance = this;
    }

    public void ReSetSpeed()
    {
        _playerScrollMultiplier = PlayerPrefs.GetFloat("scrollSpeed", 1f);
        ScrollSpeed = (unitsPerSecond / 1000f) * _playerScrollMultiplier;
    }

    private IEnumerator Start()
    {
        ReSetSpeed();

        // Wait one frame so your mod loader/chart builder has time to instantiate notes.
        yield return null;

        RebuildChartCache();

        _songDspStart = AudioSettings.dspTime;
        _audioSource.Play();
    }

    public void RebuildChartCache()
    {
        MsNote[] found = FindObjectsOfType<MsNote>(true);

        Array.Sort(found, (a, b) => a.noteTimeMs.CompareTo(b.noteTimeMs));

        _noteCount = found.Length;
        _allNotes = new MsNote[_noteCount];
        _rigidbodies = new Rigidbody2D[_noteCount];
        _colliders = new Collider2D[_noteCount];

        activeNotes.Clear();

        for (int i = 0; i < _noteCount; i++)
        {
            MsNote note = found[i];
            _allNotes[i] = note;
            _rigidbodies[i] = note != null ? note.GetComponent<Rigidbody2D>() : null;
            _colliders[i] = note != null ? note.GetComponent<Collider2D>() : null;

            if (note == null) continue;

            if (note.cachedTransform == null) note.cachedTransform = note.transform;

            // enable at approach
            note.gameObject.SetActive(false);
        }

        _nextSpawnIndex = 0;
        _initialized = true;
    }

    private void Update()
    {
        if (!_initialized) return;

        SongTimeMs = (float)((AudioSettings.dspTime - _songDspStart) * 1000.0 * _audioSource.pitch);
        JudgementTimeMs = SongTimeMs;

        _visualSongTime = Mathf.Lerp(_visualSongTime, SongTimeMs, 1f - Mathf.Exp(-Time.deltaTime * 50f));
    }

    private void LateUpdate()
    {
        if (!_initialized) return;

        SpawnUpcomingNotes();
        UpdateActiveNotes();
        CleanupNulls();
    }

    private void SpawnUpcomingNotes()
    {
        float spawnThreshold = SongTimeMs + spawnLeadMs;

        while (_nextSpawnIndex < _noteCount)
        {
            MsNote note = _allNotes[_nextSpawnIndex];

            if (note == null)
            {
                _nextSpawnIndex++;
                continue;
            }

            if (note.noteTimeMs > spawnThreshold) break;

            if (!(note.wasJudged && !note.isEvent))
            {
                if (!note.gameObject.activeSelf) note.gameObject.SetActive(true);

                if (!activeNotes.Contains(note)) activeNotes.Add(note);
            }

            _nextSpawnIndex++;
        }
    }

    private void UpdateActiveNotes()
    {
        float songTime = _visualSongTime;

        float lowerBound = songTime - visibleWindowMs;
        float upperBound = songTime + visibleWindowMs;
        float physicsLower = songTime - physicsSimWindowMs;
        float physicsUpper = songTime + physicsSimWindowMs;
        float despawnThreshold = SongTimeMs - despawnLagMs;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            MsNote note = activeNotes[i];

            if (note == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            if (note.cachedTransform == null)
                note.cachedTransform = note.transform;

            if (note.wasJudged && !note.isEvent)
            {
                note.gameObject.SetActive(false);
                activeNotes.RemoveAt(i);
                continue;
            }

            if (note.noteTimeMs < despawnThreshold)
            {
                note.gameObject.SetActive(false);
                activeNotes.RemoveAt(i);
                continue;
            }

            bool inVisibleRange = note.noteTimeMs >= lowerBound && note.noteTimeMs <= upperBound;
            bool inPhysicsWindow = note.noteTimeMs >= physicsLower && note.noteTimeMs <= physicsUpper;

            var rb = note.GetComponent<Rigidbody2D>();
            var col = note.GetComponent<Collider2D>();

            if (rb != null && rb.simulated != inPhysicsWindow) rb.simulated = inPhysicsWindow;

            if (col != null && col.enabled != inPhysicsWindow) col.enabled = inPhysicsWindow;

            if (!inVisibleRange)
            {
                //keep object active for scripts and events but disable renderer
                continue;
            }

            float y = strumLineY - (note.noteTimeMs - songTime) * ScrollSpeed;
            Vector3 cur = note.cachedTransform.localPosition;

            if (Mathf.Abs(cur.y - y) > positionEpsilon)
            {
                cur.y = y;
                note.cachedTransform.localPosition = cur;
            }
        }
    }

    private void CleanupNulls()
    {
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            if (activeNotes[i] == null) activeNotes.RemoveAt(i);
        }
    }

    public void ResetSongState()
    {
        StopAllCoroutines();

        for (int i = 0; i < activeNotes.Count; i++)
        {
            if (activeNotes[i] != null) activeNotes[i].gameObject.SetActive(false);
        }

        activeNotes.Clear();

        for (int i = 0; i < _allNotes.Length; i++)
        {
            MsNote note = _allNotes[i];
            if (note == null) continue;

            note.wasJudged = false;
            note.gameObject.SetActive(false);

            if (note.cachedTransform == null) note.cachedTransform = note.transform;
        }

        _nextSpawnIndex = 0;
        _visualSongTime = 0f;
        SongTimeMs = 0f;
        JudgementTimeMs = 0f;

        _songDspStart = AudioSettings.dspTime;

        _audioSource.Stop();
        _audioSource.Play();
    }
}