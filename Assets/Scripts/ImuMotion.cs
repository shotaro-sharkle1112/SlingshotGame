// ImuMotion.cs  — 回転＋移動を１つに統合
using System.Collections;
using UnityEngine;

public class ImuMotion : MonoBehaviour
{
    [Header("Source")]
    public PicoMpuClient Source;   // Accel[g], Gyro[deg/s]
    public Transform Target;       // 未指定なら自分

    [Header("Apply")]
    public bool applyRotation = true;
    public bool applyTranslation = true;
    public bool useRigidbody = true;

    [Header("Axis mapping (必要なら反転)")]
    public Vector3 accelSign = new Vector3(1, 1, 1);
    public Vector3 gyroSign  = new Vector3(1, 1, 1);
    static Vector3 Map(Vector3 v, Vector3 s) => new Vector3(v.x*s.x, v.y*s.y, v.z*s.z);

    // ===== 姿勢（コンプリメンタリ） =====
    [Header("Orientation")]
    [Range(0f,1f)] public float accelBlend = 0.05f;   // 傾き補正の強さ
    public bool autoGyroBiasCalibOnStart = true;
    public float biasCalibSeconds = 1.0f;
    public KeyCode keyResetYaw = KeyCode.R;
    public KeyCode keyGyroBias = KeyCode.C;

    Quaternion q = Quaternion.identity;
    Vector3 gyroBias;

    // ===== 移動（加速度積分） =====
    [Header("Translation")]
    public float accelGain = 1.0f;          // 実加速度のゲイン
    public float maxSpeed = 3.0f;           // m/s
    [Range(0,1)] public float velocityDamping = 0.02f;

    [Header("Filtering")]
    [Range(0,1)] public float accelLPF = 0.2f; // 0.15〜0.3 推奨
    public float accelDeadband = 0.08f;        // m/s^2

    [Header("ZUPT (静止判定で速度抑制)")]
    public float gyroStationaryThresh = 2.0f;  // deg/s
    public float accel1gThresh = 0.05f;        // | |a|-1g |
    [Range(0,1)] public float zuptDamping = 0.85f;

    Vector3 velocity;
    Vector3 aWorldLPF;
    Rigidbody rb;

    public Quaternion CurrentOrientation => q; // 外からも読めるように

    void Awake() {
        if (Target == null) Target = transform;
        rb = Target.GetComponent<Rigidbody>();
    }

    void OnEnable() {
        // Accelで初期傾き（yaw=0）に合わせる
        if (Source != null) {
            var a = Map(Source.Accel, accelSign);
            if (a.sqrMagnitude > 1e-6f) {
                var upMeas = (-a).normalized;
                q = Quaternion.FromToRotation(Vector3.up, upMeas);
            }
        }
        if (autoGyroBiasCalibOnStart) StartCoroutine(CalibGyroBias());
    }

    IEnumerator CalibGyroBias() {
        gyroBias = Vector3.zero;
        float t = 0f; int n = 0;
        while (t < biasCalibSeconds) {
            if (Source != null) { gyroBias += Map(Source.Gyro, gyroSign); n++; }
            yield return null; t += Time.deltaTime;
        }
        if (n > 0) gyroBias /= n;
        ResetYaw(); // キャリブ後はyawリセットが無難
    }

    void Update() {
        if (Input.GetKeyDown(keyGyroBias)) StartCoroutine(CalibGyroBias());
        if (Input.GetKeyDown(keyResetYaw)) ResetYaw();
    }

    void FixedUpdate() {
        if (Source == null) return;
        float dt = Mathf.Max(Time.fixedDeltaTime, 1e-4f);

        // ==== 1) 姿勢更新（ジャイロ積分） ====
        Vector3 gyro = Map(Source.Gyro, gyroSign) - gyroBias; // deg/s
        float ang = gyro.magnitude * dt;
        if (ang > 0f) q = q * Quaternion.AngleAxis(ang, gyro.normalized);

        // ==== 2) 加速度で傾き補正（yaw以外） ====
        Vector3 a = Map(Source.Accel, accelSign);
        if (a.sqrMagnitude > 1e-6f) {
            Vector3 upPred = q * Vector3.up;
            Vector3 upMeas = (-a).normalized;
            Vector3 axis = Vector3.Cross(upPred, upMeas);
            float s = axis.magnitude;
            if (s > 1e-6f) {
                float errDeg = Mathf.Asin(Mathf.Clamp(s, -1f, 1f)) * Mathf.Rad2Deg;
                q = Quaternion.AngleAxis(errDeg * accelBlend, axis / s) * q;
            }
        }

        if (applyRotation) Target.rotation = q;

        // ==== 3) 移動：実加速度 → 速度 → 位置 ====
        if (applyTranslation) {
            Vector3 f_body = a * 9.80665f;          // 比力[g]→m/s^2
            Vector3 f_world = q * f_body;           // worldへ
            Vector3 a_world = (f_world + Physics.gravity) * accelGain;

            aWorldLPF = Vector3.Lerp(aWorldLPF, a_world, accelLPF);
            if (aWorldLPF.magnitude < accelDeadband) aWorldLPF = Vector3.zero;

            bool stationary =
                Source.Gyro.magnitude < gyroStationaryThresh &&
                Mathf.Abs(Source.Accel.magnitude - 1.0f) < accel1gThresh;

            if (stationary) {
                velocity *= zuptDamping;
            } else {
                velocity += aWorldLPF * dt;
                velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
                velocity *= (1f - Mathf.Clamp01(velocityDamping));
            }

            Vector3 dp = velocity * dt;
            if (useRigidbody && rb != null) rb.MovePosition(rb.position + dp);
            else Target.position += dp;
        }
    }

    void ResetYaw() {
        // pitch/rollは加速度で決定、yawは0に
        var a = Map(Source.Accel, accelSign);
        if (a.sqrMagnitude > 1e-6f) {
            var upMeas = (-a).normalized;
            q = Quaternion.FromToRotation(Vector3.up, upMeas);
        }
    }
}
