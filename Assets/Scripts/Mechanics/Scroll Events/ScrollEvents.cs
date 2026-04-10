using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameManager;
using static PostProcessingManager;

public enum TypeOfScrollEvent
{
    FocusCentre,
    FocusPlayerRight,
    FocusPlayerLeft,
    CameraFov,
    ChangeSongSpeed,
    SectionCompleteAnimation,
    Cutscene,
    Animation,
    InstantRestart,
    RepeatedTile,
    RotateTile,
    MoveTiles,
    PostProcessEffect,
    AfterImageEffect
}

public class ScrollEvents : MonoBehaviour
{
    public TypeOfScrollEvent typeOfScrollEvent;

    [Header("Camera FOV")]
    public float ZoomAmount;
    public float ZoomSpeed;
    public bool BpmBump;

    [Header("Repeated Tile")]
    public float RepeatRate;
    public float RepeatTime;

    [Header("Rotate Tile")]
    public string Axis;
    public float RotateAmount;
    public float RotateTime;

    [Header("Move Tile")]
    public float MoveAmount;
    public float MoveTime;

    [Header("Post Process")]
    public string PostProcessEffectName;
    public float PostProcessEffectValue;
    public float PostProcessEffectSpeed;

    [Header("After Image")]
    public string whichPlayerToAfterImage;
    public bool displayAfterImage;
    public float afterImageSpeed;
    public float afterImageColourR;
    public float afterImageColourG;
    public float afterImageColourB;
    public float afterImageColourA;
    public float afterImageDuration;
    public int afterImageZIndex;
    public bool flipXAfterImage;
    public bool flipYAfterImage;

    [SerializeField] private float scrollSpeedModificationAmount;

    [Header("Animation")]
    [Tooltip("if this event doesn't make use of animations, don't drag one into this variable.")]
    [SerializeField] private Animation eventAnim;

    [Header("Cutscene")]
    [SerializeField] private string _cutscenePath;

    private bool _fired = false;

    private void FocusCentre() => Instance.focus = Focus.Centre;
    private void FocusLeftPlayer() => Instance.focus = Focus.LeftPlayer;
    private void FocusRightPlayer() => Instance.focus = Focus.RightPlayer;

    private void CameraFov()
    {
        Instance.CameraFov = ZoomAmount;
        Instance.CameraFovSpeed = ZoomSpeed;
        Instance.BpmBump = BpmBump;
    }

    private RepeatedZoom GetRepeatedZoom()
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("ScrollEvents: Camera.main is null, could not find RepeatedZoom.");
            return null;
        }

        Transform t = Camera.main.transform;

        if (t.parent == null || t.parent.parent == null || t.parent.parent.parent == null)
        {
            Debug.LogWarning("ScrollEvents: Camera hierarchy is not deep enough to find RepeatedZoom.");
            return null;
        }

        RepeatedZoom rz = t.parent.parent.parent.GetComponent<RepeatedZoom>();

        if (rz == null) Debug.LogWarning("ScrollEvents: RepeatedZoom component was not found on expected camera parent.");

        return rz;
    }

    private void CameraZoomRepeat(float repeatValue, float smoothing)
    {
        RepeatedZoom rz = GetRepeatedZoom();
        if (rz == null) return;

        rz.targetZoom = repeatValue;
        rz.zoomSmoothTime = smoothing;
    }

    private void CameraRotateTile(float rotateAmount, float smoothing)
    {
        RepeatedZoom rz = GetRepeatedZoom();
        if (rz == null) return;

        rz.targetZ = rotateAmount;
        rz.zSmoothTime = smoothing;
    }

    private void CameraMoveTile(string axis, float moveAmount, float smoothing)
    {
        RepeatedZoom rz = GetRepeatedZoom();
        if (rz == null) return;

        axis = (axis ?? "").Trim().ToUpperInvariant();

        if (axis == "X")
        {
            rz.targetX = moveAmount;
            rz.xSmoothTime = smoothing;
        }
        else if (axis == "Y")
        {
            rz.targetY = moveAmount;
            rz.ySmoothTime = smoothing;
        }
        else Debug.LogWarning($"ScrollEvents: Unsupported axis '{axis}' for MoveTiles. Use 'X' or 'Y'.");
    }

    private void PostProcessEffect(string effect, float value, float speed) => PP_Instance.SetEffect(effect, value, speed);

    private void AfterImageEffect(string player, bool display, bool flipX, bool flipY, int zIndex, float duration, float speed, float R, float G, float B, float A)
    {
        player = (player ?? "").Trim().ToLowerInvariant();

        bool isLeft = (player == "left" || player == "leftplayer");
        bool isRight = (player == "right" || player == "rightplayer");

        float nr = (R > 1f) ? (R / 255f) : R;
        float ng = (G > 1f) ? (G / 255f) : G;
        float nb = (B > 1f) ? (B / 255f) : B;
        float na = (A > 1f) ? (A / 255f) : A;

        Color col = new Color(Mathf.Clamp01(nr), Mathf.Clamp01(ng), Mathf.Clamp01(nb), Mathf.Clamp01(na));

        if (isLeft)
        {
            var s = Instance._aiScript.GetComponent<DreamwaveAICharacter>();
            if (s == null) return;

            s.afterImage = display;
            s.afterImageColour = col;
            s.afterImageSpeed = speed;
            s.afterImageDuration = duration;
            s.afterImageZIndex = zIndex;
            s.flipXAfterImage = flipX;
            s.flipYAfterImage = flipY;
        }
        else if (isRight)
        {
            var s = Instance._playerScript.GetComponent<DreamwaveCharacter>();
            if (s == null) return;

            s.afterImage = display;
            s.afterImageColour = col;
            s.afterImageSpeed = speed;
            s.afterImageDuration = duration;
            s.afterImageZIndex = zIndex;
            s.flipXAfterImage = flipX;
            s.flipYAfterImage = flipY;
        }
        else Debug.LogWarning($"ScrollEvents: Unknown afterimage player '{player}'. Use left/right or leftplayer/rightplayer.");
    }

    private void ChangeSongSpeed() => Instance.scrollManager.scrollSpeedMultiplier = scrollSpeedModificationAmount;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("ScrollEventTrigger")) return;
        if (_fired) return;

        FireEvent(typeOfScrollEvent);
        _fired = true;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.gameObject.CompareTag("ScrollEventTrigger")) return;
        if (_fired) return;

        FireEvent(typeOfScrollEvent);
        _fired = true;
    }

    private void FireEvent(TypeOfScrollEvent ev)
    {
        switch (ev)
        {
            case TypeOfScrollEvent.FocusCentre: FocusCentre(); break;
            case TypeOfScrollEvent.FocusPlayerRight: FocusRightPlayer(); break;
            case TypeOfScrollEvent.FocusPlayerLeft: FocusLeftPlayer(); break;
            case TypeOfScrollEvent.CameraFov: CameraFov(); break;
            case TypeOfScrollEvent.ChangeSongSpeed: ChangeSongSpeed(); break;
            case TypeOfScrollEvent.Animation: if (eventAnim != null) eventAnim.Play(); break;
            case TypeOfScrollEvent.InstantRestart: SceneManager.LoadScene(SceneManager.GetActiveScene().name); break;
            case TypeOfScrollEvent.Cutscene: Instance.DreamwaveVideoStreamer.InitLoad(_cutscenePath); break;
            case TypeOfScrollEvent.RepeatedTile: CameraZoomRepeat(RepeatRate, RepeatTime); break;
            case TypeOfScrollEvent.RotateTile: CameraRotateTile(RotateAmount, RotateTime); break;
            case TypeOfScrollEvent.MoveTiles: CameraMoveTile(Axis, MoveAmount, MoveTime); break;
            case TypeOfScrollEvent.PostProcessEffect: PostProcessEffect(PostProcessEffectName, PostProcessEffectValue, PostProcessEffectSpeed); break;
            case TypeOfScrollEvent.AfterImageEffect: AfterImageEffect(whichPlayerToAfterImage, displayAfterImage, flipXAfterImage, flipYAfterImage, afterImageZIndex, afterImageDuration, afterImageSpeed, afterImageColourR, afterImageColourG, afterImageColourB, afterImageColourA); break;
        }
    }
}