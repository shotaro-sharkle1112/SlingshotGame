using Unity.Properties;
using UnityEngine;

public class TargetMover : MonoBehaviour
{
    public TargetMoveType moveType;
    public float speed;
    public float radius;
    public float angularSpeed;
    public float startAngle;
    public float lifetime;

    private Vector3 origin;
    private float elapsed;
    private float angleDeg;
    private Camera cam;

    public void Init(SpawnEntry entry)
    {
        moveType = entry.moveType;
        speed = entry.speed;
        radius = entry.radius;
        angularSpeed = entry.angularSpeed;
        startAngle = entry.startAngle;
        lifetime = entry.lifetime;

        origin = transform.position;
        angleDeg = startAngle;
        cam = Camera.main;

        if (moveType == TargetMoveType.Circular)
        {
            transform.position = origin + OrbitOffset(angleDeg);
        }
    }

    // Update is called once per frame
    void Update()
    {
        elapsed += Time.deltaTime;

        switch (moveType)
        {
            case TargetMoveType.Horizontal:
                transform.position += Vector3.right * speed * Time.deltaTime;
                break;
            case TargetMoveType.Vertical:
                transform.position += Vector3.up * speed * Time.deltaTime;
                break;
            case TargetMoveType.Circular:
                angleDeg += angularSpeed * Time.deltaTime;
                transform.position = origin + OrbitOffset(angleDeg);
                break;    
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private Vector3 OrbitOffset(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
    }

    private bool IsOffScreen()
    {
        if (cam == null) return false;
        Vector3 vp = cam.WorldToViewportPoint(transform.position);
        if (vp.z < 0f) return true;
        const float margin = 0.1f;
        return vp.x < -margin || vp.x > 1f + margin || vp.y < -margin || vp.y > 1f + margin;
    }
}
