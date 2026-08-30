// ImuTranslationSmooth.cs
// 重力補正した加速度を積分して移動（スムージング・ZUPT付き）
using UnityEngine;

public class ImuTranslation : MonoBehaviour
{
    [Header("Sources")]
    public PicoMpuClient Source;       // Accel(g) / Gyro(deg/s)
    public ImuOrientation Orientation; // 姿勢（Quaternion）。ImuOrientationに public Quaternion CurrentOrientation => q; を追加してね
    public Transform Target;           // 未指定なら自分

    [Header("Physics")]
    public bool useRigidbody = true;   // Rigidbodyがあるならtrue推奨
    public float accelGain = 1.0f;     // 実加速度に掛けるゲイン
    public float maxSpeed = 3.0f;      // 速度上限[m/s]
    [Range(0,1)] public float velocityDamping = 0.02f; // 毎ステップ減衰(0..1)

    [Header("Filtering")]
    [Range(0,1)] public float accelLPF = 0.2f; // 0=効かない, 1=瞬間追従（0.15〜0.3目安）
    public float accelDeadband = 0.08f; // [m/s^2] この以下はゼロ扱い（0.05〜0.15）

    [Header("ZUPT (止まってる判定で速度抑制)")]
    public float gyroStationaryThresh = 2.0f; // [deg/s]
    public float accel1gThresh = 0.05f;       // | |a|-1g | < しきい で静止
    [Range(0,1)] public float zuptDamping = 0.85f; // 静止中の速度減衰率

    private Vector3 velocity;   // world vel [m/s]
    private Vector3 aWorldLPF;  // LPF後の加速度
    private Rigidbody rb;

    void Awake() {
        if (Target == null) Target = transform;
        rb = Target.GetComponent<Rigidbody>();
    }

    void FixedUpdate()  // 物理はFixedUpdateで
    {
        if (Source == null || Orientation == null) return;
        float dt = Mathf.Max(Time.fixedDeltaTime, 1e-4f);

        // 1) センサの比力 f_body = Accel[g] * 9.80665
        Vector3 f_body = Source.Accel * 9.80665f;

        // 2) 姿勢で world に回す
        Quaternion R = Orientation.CurrentOrientation; // body→world
        Vector3 f_world = R * f_body;

        // 3) 実加速度 a = f + g
        Vector3 a_world = f_world + Physics.gravity;
        a_world *= accelGain;

        // 4) 低域フィルタ + デッドバンド
        aWorldLPF = Vector3.Lerp(aWorldLPF, a_world, accelLPF);
        if (aWorldLPF.magnitude < accelDeadband) aWorldLPF = Vector3.zero;

        // 5) ZUPT（止まってるとき速度を落とす）
        bool stationary =
            Source.Gyro.magnitude < gyroStationaryThresh &&
            Mathf.Abs(Source.Accel.magnitude - 1.0f) < accel1gThresh;

        if (stationary) {
            velocity *= zuptDamping; // スッと止める
        } else {
            velocity += aWorldLPF * dt;
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
            velocity *= (1f - Mathf.Clamp01(velocityDamping));
        }

        // 6) 位置適用
        Vector3 dp = velocity * dt;
        if (useRigidbody && rb != null) {
            rb.MovePosition(rb.position + dp);
        } else {
            Target.position += dp;
        }
    }
}
