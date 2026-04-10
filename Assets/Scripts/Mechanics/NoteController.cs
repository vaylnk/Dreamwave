using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteController : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private SpriteRenderer[] noteRenderers;
    [SerializeField] public List<Sprite> noteSpritesDown;
    [SerializeField] public List<Sprite> noteSpritesRelease;
    [SerializeField] private Animator plrAnim;
    [SerializeField] private string currentAnimationPlaying;

    [Header("Animation Settings")]
    [SerializeField] private float noteAnimSpeed = 0.1f;

    private Coroutine[] downCoroutines;
    private Coroutine[] releaseCoroutines;

    private void Awake()
    {
        downCoroutines = new Coroutine[noteRenderers.Length];
        releaseCoroutines = new Coroutine[noteRenderers.Length];

        if (Application.platform == RuntimePlatform.Android)
        {
            for (int i = 0; i < noteRenderers.Length; i++)
            {
                noteRenderers[i].enabled = false;
            }
        }
    }

    private void OnEnable()
    {
        PauseMenu.Pause += OnPause;
        NoteHitbox.BotLanePressed += OnBotLanePressed;
        NoteHitbox.BotLaneReleased += OnBotLaneReleased;
    }

    private void OnDisable()
    {
        PauseMenu.Pause -= OnPause;
        NoteHitbox.BotLanePressed -= OnBotLanePressed;
        NoteHitbox.BotLaneReleased -= OnBotLaneReleased;
    }

    private void OnDestroy()
    {
        PauseMenu.Pause -= OnPause;
        NoteHitbox.BotLanePressed -= OnBotLanePressed;
        NoteHitbox.BotLaneReleased -= OnBotLaneReleased;
    }

    private void Update()
    {
        if (PauseMenu.instance._isPaused) return;

        if (PlayerPrefs.GetInt("botPlay") == 0) ControlInput();
    }

    private void ControlInput()
    {
        if (Input.GetKeyDown(GameManager.Instance.left)) PlayPressed(0);
        else if (Input.GetKeyUp(GameManager.Instance.left)) PlayReleased(0);

        if (Input.GetKeyDown(GameManager.Instance.down)) PlayPressed(1);
        else if (Input.GetKeyUp(GameManager.Instance.down)) PlayReleased(1);

        if (Input.GetKeyDown(GameManager.Instance.up)) PlayPressed(2);
        else if (Input.GetKeyUp(GameManager.Instance.up)) PlayReleased(2);

        if (Input.GetKeyDown(GameManager.Instance.right)) PlayPressed(3);
        else if (Input.GetKeyUp(GameManager.Instance.right)) PlayReleased(3);
    }

    private void OnBotLanePressed(WhichSide side)
    {
        if (PlayerPrefs.GetInt("botPlay") == 0) return;

        int index = SideToIndex(side);
        if (index < 0 || index >= noteRenderers.Length) return;

        PlayPressed(index);
    }

    private void OnBotLaneReleased(WhichSide side)
    {
        if (PlayerPrefs.GetInt("botPlay") == 0) return;

        int index = SideToIndex(side);
        if (index < 0 || index >= noteRenderers.Length) return;

        PlayReleased(index);
    }

    private int SideToIndex(WhichSide side)
    {
        switch (side)
        {
            case WhichSide.Left: return 0;
            case WhichSide.Down: return 2;
            case WhichSide.Up: return 1;
            case WhichSide.Right: return 3;
            default: return -1;
        }
    }

    private void PlayPressed(int index)
    {
        if (releaseCoroutines[index] != null)
        {
            StopCoroutine(releaseCoroutines[index]);
            releaseCoroutines[index] = null;
        }

        if (downCoroutines[index] != null) StopCoroutine(downCoroutines[index]);

        downCoroutines[index] = StartCoroutine(KeyDownSpriteFlick(index));
    }

    private void PlayReleased(int index)
    {
        if (downCoroutines[index] != null)
        {
            StopCoroutine(downCoroutines[index]);
            downCoroutines[index] = null;
        }

        if (releaseCoroutines[index] != null) StopCoroutine(releaseCoroutines[index]);

        releaseCoroutines[index] = StartCoroutine(KeyReleaseSpriteFlick(index));
    }

    private IEnumerator KeyDownSpriteFlick(int index)
    {
        for (int i = 0; i < noteSpritesDown.Count; i++)
        {
            noteRenderers[index].sprite = noteSpritesDown[i];

            yield return new WaitForSecondsRealtime(noteAnimSpeed);
        }

        noteRenderers[index].sprite = noteSpritesDown[noteSpritesDown.Count - 1];
        downCoroutines[index] = null;
    }

    private IEnumerator KeyReleaseSpriteFlick(int index)
    {
        for (int i = 0; i < noteSpritesRelease.Count; i++)
        {
            noteRenderers[index].sprite = noteSpritesRelease[i];

            yield return new WaitForSecondsRealtime(noteAnimSpeed);
        }

        noteRenderers[index].sprite = noteSpritesRelease[noteSpritesRelease.Count - 1];
        releaseCoroutines[index] = null;
    }

    private void OnPause(bool paused)
    {
        if (!paused) return;

        StopAllCoroutines();

        for (int i = 0; i < downCoroutines.Length; i++) downCoroutines[i] = null;

        for (int i = 0; i < releaseCoroutines.Length; i++) releaseCoroutines[i] = null;
    }
}