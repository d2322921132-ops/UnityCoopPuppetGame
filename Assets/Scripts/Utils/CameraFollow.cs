using UnityEngine;

/// <summary>
/// 相机跟随组件 - 平滑跟随玩家
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    [SerializeField] private Transform target;

    [Header("跟随参数")]
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -8);
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float rotationSpeed = 3f;

    [Header("边界限制")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    [Header("移动端适配")]
    [SerializeField] private float mobileHeightMultiplier = 1.2f;

    private Vector3 currentVelocity;
    private bool isFollowing;

    private void Start()
    {
#if UNITY_ANDROID || UNITY_IOS
        offset.y *= mobileHeightMultiplier;
#endif
    }

    private void LateUpdate()
    {
        if (!isFollowing || target == null) return;

        Vector3 desiredPosition = target.position + offset;

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minBounds.y, maxBounds.y);
        }

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            1f / smoothSpeed
        );

        transform.position = smoothedPosition;

        if (rotationSpeed > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        isFollowing = true;

        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }

    public void ClearTarget()
    {
        target = null;
        isFollowing = false;
    }

    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }

    public void SetSmoothSpeed(float speed)
    {
        smoothSpeed = speed;
    }
}
