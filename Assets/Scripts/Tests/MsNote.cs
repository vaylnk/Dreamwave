using UnityEngine;

public class MsNote : MonoBehaviour
{
    public float noteTimeMs;
    public float receptorY;

    [HideInInspector] public Transform cachedTransform;
    [HideInInspector] public bool wasJudged = false;
    public bool isEvent = false;

    void Awake()
    {
        cachedTransform = transform;
    }
}
