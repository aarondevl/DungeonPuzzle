using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionCone : MonoBehaviour
{
    [SerializeField] public float angle = 60f;
    [SerializeField] public float distance = 5f;
    [SerializeField] int rayCount = 30;
    [SerializeField] LayerMask wallLayer;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] Material normalMaterial;
    [SerializeField] Material alertMaterial;

    MeshFilter _mf;
    MeshRenderer _mr;
    Mesh _mesh;

    public event System.Action OnPlayerDetected;

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
        _mesh = new Mesh();
        _mf.mesh = _mesh;
        _mr.material = normalMaterial;
    }

    void Update()
    {
        BuildMesh();
        CheckDetection();
    }

    void BuildMesh()
    {
        float halfAngle = angle / 2f;
        float angleStep = angle / rayCount;

        Vector3[] vertices = new Vector3[rayCount + 2];
        int[] triangles = new int[rayCount * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i <= rayCount; i++)
        {
            float currentAngle = -halfAngle + angleStep * i;
            Vector2 dir = DirFromAngle(currentAngle);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, distance, wallLayer);
            Vector3 point = hit ? (Vector3)hit.point - transform.position
                                : (Vector3)(dir * distance);
            vertices[i + 1] = point;
        }

        for (int i = 0; i < rayCount; i++)
        {
            triangles[i * 3 + 0] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
    }

    void CheckDetection()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, distance, playerLayer);
        if (hit == null) return;

        Vector2 toPlayer = hit.transform.position - transform.position;
        float angleTo = Vector2.Angle(transform.up, toPlayer);
        if (angleTo < angle / 2f)
        {
            RaycastHit2D los = Physics2D.Raycast(transform.position, toPlayer.normalized, distance, wallLayer);
            if (!los) OnPlayerDetected?.Invoke();
        }
    }

    public void SetAlerted(bool alerted) =>
        _mr.material = alerted ? alertMaterial : normalMaterial;

    Vector2 DirFromAngle(float angleDeg)
    {
        float rad = (transform.eulerAngles.z + angleDeg) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
    }
}
