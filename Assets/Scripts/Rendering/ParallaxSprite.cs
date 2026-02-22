using UnityEngine;

public class ParallaxSprite : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float distanceMultiplier = 0.1f;

    private Vector3 startPosition;
    private Vector3 cameraStartPosition;

    void Start()
    {
        startPosition = transform.position;
        cameraStartPosition = Camera.main.transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        PositionBasedOnZIndex();
    }

    private void PositionBasedOnZIndex()
    {
        Vector3 cameraDelta = Camera.main.transform.position - cameraStartPosition;

        float depthFactor = Mathf.Abs(spriteRenderer.sortingOrder) * distanceMultiplier;

        transform.position = startPosition + cameraDelta * depthFactor;
    }
}
