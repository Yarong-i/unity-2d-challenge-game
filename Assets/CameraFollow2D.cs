using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 targetViewportPosition = new Vector2(0.5f, 0.38f);
    [SerializeField] private Vector3 worldOffset = Vector3.zero;
    [SerializeField] private float smoothTime = 0.12f;
    [SerializeField] private float maxFollowSpeed = 30f;
    [SerializeField] private bool snapOnStart = true;

    private const string FallbackTargetName = "player";
    private const float MinimumTargetViewportY = 0.32f;

    private Camera followCamera;
    private Vector3 followVelocity;
    private bool hasSnapped;

    private void Awake()
    {
        followCamera = GetComponent<Camera>();
        ApplyDefaultCompositionIfNeeded();
        ResolveTargetIfNeeded();
    }

    private void OnValidate()
    {
        ApplyDefaultCompositionIfNeeded();
        smoothTime = Mathf.Max(0.01f, smoothTime);
        maxFollowSpeed = Mathf.Max(0.01f, maxFollowSpeed);
    }

    private void LateUpdate()
    {
        ResolveTargetIfNeeded();
        if (target == null)
            return;

        Vector3 currentPosition = transform.position;
        Vector3 desiredPosition = GetDesiredPosition(currentPosition);

        if (snapOnStart && !hasSnapped)
        {
            transform.position = desiredPosition;
            followVelocity = Vector3.zero;
            hasSnapped = true;
            return;
        }

        Vector3 nextPosition = Vector3.SmoothDamp(
            currentPosition,
            desiredPosition,
            ref followVelocity,
            smoothTime,
            maxFollowSpeed,
            Time.deltaTime);

        nextPosition.z = currentPosition.z;
        transform.position = nextPosition;
        hasSnapped = true;
    }

    private void ResolveTargetIfNeeded()
    {
        if (target != null)
            return;

        GameObject fallbackTarget = GameObject.Find(FallbackTargetName);
        if (fallbackTarget != null)
            target = fallbackTarget.transform;
    }

    private Vector3 GetDesiredPosition(Vector3 currentPosition)
    {
        Vector3 targetPosition = target.position + worldOffset;
        Vector3 desiredPosition = targetPosition;

        if (followCamera != null && followCamera.orthographic)
        {
            Vector2 visibleExtents = GetVisibleExtents();
            Vector2 viewportOffsetFromCenter = new Vector2(
                (targetViewportPosition.x - 0.5f) * visibleExtents.x * 2f,
                (targetViewportPosition.y - 0.5f) * visibleExtents.y * 2f);

            desiredPosition.x = targetPosition.x - viewportOffsetFromCenter.x;
            desiredPosition.y = targetPosition.y - viewportOffsetFromCenter.y;
        }

        desiredPosition.z = currentPosition.z;
        return desiredPosition;
    }

    private Vector2 GetVisibleExtents()
    {
        float verticalExtent = followCamera.orthographicSize;
        return new Vector2(verticalExtent * followCamera.aspect, verticalExtent);
    }

    private void ApplyDefaultCompositionIfNeeded()
    {
        if (targetViewportPosition == Vector2.zero)
            targetViewportPosition = new Vector2(0.5f, 0.38f);

        targetViewportPosition.x = Mathf.Clamp01(targetViewportPosition.x);
        targetViewportPosition.y = Mathf.Clamp(targetViewportPosition.y, MinimumTargetViewportY, 1f);
    }
}
