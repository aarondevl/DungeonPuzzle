using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class GalleryCameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Rect worldBounds = new(-32f, -20f, 64f, 42f);
    [SerializeField, Min(0.01f)] float sharpness = 6f;

    Camera _camera;

    void Awake() => _camera = GetComponent<Camera>();

    void LateUpdate()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            target = player.transform;
        }

        Vector2 center = ClampCenter(target.position, worldBounds,
            _camera.orthographicSize, _camera.aspect);
        var desired = new Vector3(center.x, center.y, transform.position.z);
        float t = 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, t);
    }

    public static Vector2 ClampCenter(Vector2 desired, Rect bounds, float halfHeight, float aspect)
    {
        float halfWidth = halfHeight * Mathf.Max(0.01f, aspect);
        float minX = bounds.xMin + halfWidth;
        float maxX = bounds.xMax - halfWidth;
        float minY = bounds.yMin + halfHeight;
        float maxY = bounds.yMax - halfHeight;
        float x = minX <= maxX ? Mathf.Clamp(desired.x, minX, maxX) : bounds.center.x;
        float y = minY <= maxY ? Mathf.Clamp(desired.y, minY, maxY) : bounds.center.y;
        return new Vector2(x, y);
    }
}
