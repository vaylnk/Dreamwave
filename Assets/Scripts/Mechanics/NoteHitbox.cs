using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class NoteHitbox : MonoBehaviour
{
    public WhichSide side;

    public List<MsNote> notesWithinHitBox = new();

    // timing windows - ms
    public float[] ratingThresholds = new float[5];

    public delegate void HitNote(string scoreType, float msDelay, float dummy, string direction);
    public static event HitNote NoteHit;

    public delegate void BotLaneInput(WhichSide side);
    public static event BotLaneInput BotLanePressed;
    public static event BotLaneInput BotLaneReleased;

    public KeyCode keyForSide;
    public string buttonForSide;
    public GameObject NoteHitParticle;

    [Header("Bot Visuals")]
    [SerializeField] private float botLaneReleaseDelay = 0.05f;

    private Coroutine botReleaseRoutine;

    private void Awake()
    {
        ratingThresholds[0] = 40f;   // Dreamy
        ratingThresholds[1] = 60f;   // Sick
        ratingThresholds[2] = 80f;   // Cool
        ratingThresholds[3] = 110f;  // Bad
        ratingThresholds[4] = 130f;  // Shit / Miss cutoff
    }

    private void Start()
    {
        buttonForSide = side.ToString();
        PlayerPrefs.SetInt("botPlay", 1);
    }

    private void Update()
    {
        if (PauseMenu.instance._isPaused) return;

        float songTime = StrumManager.SM_Instance.JudgementTimeMs;

        // miss handling - late notes
        for (int i = notesWithinHitBox.Count - 1; i >= 0; i--)
        {
            var note = notesWithinHitBox[i];

            if (note == null || note.wasJudged)
            {
                notesWithinHitBox.RemoveAt(i);
                continue;
            }

            float delta = songTime - note.noteTimeMs;

            if (delta > ratingThresholds[4])
            {
                Judge(note, "Missed", delta, true);
                notesWithinHitBox.RemoveAt(i);
            }
        }

        if (notesWithinHitBox.Count == 0) return;

        if (PlayerPrefs.GetInt("botPlay") == 0)
        {
            if (Input.GetKeyDown(keyForSide) || MobileControls.instance.GetButtonsPressed(buttonForSide)) TryHit();
        }
        else TryHitBot();
    }

    private void TryHit()
    {
        float songTime = StrumManager.SM_Instance.JudgementTimeMs;

        MsNote earliest = null;

        foreach (var note in notesWithinHitBox)
        {
            if (note == null || note.wasJudged) continue;

            if (earliest == null || note.noteTimeMs < earliest.noteTimeMs) earliest = note;
        }

        if (earliest == null) return;

        float signedDelta = songTime - earliest.noteTimeMs;

        // early-hit protection
        if (signedDelta < -ratingThresholds[4]) return;

        float absDelta = Mathf.Abs(signedDelta);
        string rating = GetRating(absDelta);

        Judge(earliest, rating, absDelta, false);
        notesWithinHitBox.Remove(earliest);
    }

    private void TryHitBot()
    {
        float songTime = StrumManager.SM_Instance.JudgementTimeMs;

        MsNote earliest = null;

        foreach (var note in notesWithinHitBox)
        {
            if (note == null || note.wasJudged) continue;

            if (earliest == null || note.noteTimeMs < earliest.noteTimeMs) earliest = note;
        }

        if (earliest == null) return;

        float signedDelta = songTime - earliest.noteTimeMs;

        // wait until note time is reached
        if (signedDelta < 0f) return;

        Judge(earliest, "Dreamy", 0f, false);
        notesWithinHitBox.Remove(earliest);

        EmitBotPressAndRelease();
    }

    private void EmitBotPressAndRelease()
    {
        if (PlayerPrefs.GetInt("botPlay") == 0) return;

        BotLanePressed?.Invoke(side);

        if (botReleaseRoutine != null) StopCoroutine(botReleaseRoutine);

        botReleaseRoutine = StartCoroutine(BotReleaseAfterDelay());
    }

    private IEnumerator BotReleaseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(botLaneReleaseDelay);

        if (PlayerPrefs.GetInt("botPlay") != 0) BotLaneReleased?.Invoke(side);

        botReleaseRoutine = null;
    }

    private void Judge(MsNote note, string rating, float delta, bool isMiss)
    {
        note.wasJudged = true;
        note.enabled = false;

        var sr = note.GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        NoteHit?.Invoke(rating, delta, 0f, keyForSide.ToString());

        if (!isMiss && (rating == "Dreamy" || rating == "Sick" || rating == "Cool") && GameManager.Instance.shouldDrawNoteSplashes)
        {
            Instantiate(NoteHitParticle, NoteHitParticle.transform.position, Quaternion.identity).SetActive(true);
        }
    }

    private string GetRating(float delta)
    {
        if (delta <= ratingThresholds[0]) return "Dreamy";
        if (delta <= ratingThresholds[1]) return "Sick";
        if (delta <= ratingThresholds[2]) return "Cool";
        if (delta <= ratingThresholds[3]) return "Bad";
        if (delta <= ratingThresholds[4]) return "Shit";
        return "Missed";
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var note = collision.GetComponent<MsNote>();
        if (note == null || note.wasJudged) return;

        if (!notesWithinHitBox.Contains(note))
        {
            notesWithinHitBox.Add(note);
            notesWithinHitBox.Sort((a, b) => a.noteTimeMs.CompareTo(b.noteTimeMs));
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var note = collision.GetComponent<MsNote>();
        if (note == null || note.wasJudged) return;

        notesWithinHitBox.Remove(note);
    }

    private void OnDisable()
    {
        if (botReleaseRoutine != null)
        {
            StopCoroutine(botReleaseRoutine);
            botReleaseRoutine = null;
        }
    }
}