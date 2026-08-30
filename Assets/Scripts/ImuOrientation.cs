// ImuOrientation.cs
// 取得した accel[g], gyro[deg/s] から姿勢を推定して target を回転
using System.Collections;
using UnityEngine;

public class ImuOrientation : MonoBehaviour
{
    [Header("Source (PicoSerialReceiver)")]
    public PicoSerialReceiver Source;      // Accel, Gyro を読む
    public Transform Target;               // 回す対象（未指定なら自分）

    [Header("Filter")]
    [Range(0f, 1f)] public float accelBlend = 0.05f; // 加速度での傾き補正の強さ（0.02〜0.1 目安）
    public bool autoGyroBiasCalibOnStart = true;     // 起動時にジャイロのゼロ点を測る（静止で）
    public float biasCalibSeconds = 1.0f;            // 何秒平均するか（静止状態で）

    [Header("Axis mapping (Unity側: x=右, y=上, z=前 の符号反転)")]
    [Tooltip("Unity軸の符号反転。x=右, y=上, z=前。上下逆ならyを-1に")]
    public Vector3 accelSign = new Vector3(1, 1, 1);
    [Tooltip("Unity軸の符号反転。x=右, y=上, z=前")]
    public Vector3 gyroSign  = new Vector3(1, 1, 1);

    public enum ImuAxis { X, Y, Z }
    [Header("IMU軸 → Unity軸 の対応 (IMU側: どの軸が何に相当するか)")]
    [Tooltip("IMUのどの軸がUnityの上方向(Y)に相当するか。静止時に1gが出る軸を選ぶ")]
    public ImuAxis imuUpAxis = ImuAxis.Z;
    [Tooltip("IMUのどの軸がUnityの前方向(Z)に相当するか")]
    public ImuAxis imuForwardAxis = ImuAxis.Y;
    [Tooltip("IMUのどの軸がUnityの右方向(X)に相当するか")]
    public ImuAxis imuRightAxis = ImuAxis.X;

    [Header("Smoothing (追従の滑らかさ)")]
    [Tooltip("目標への追従時間（秒）。大きいほど滑らか")]
    public float smoothTime = 0.12f;
    [Tooltip("1秒あたりの最大回転速度（度）")]
    public float maxDegPerSec = 120f;
    [Tooltip("デッドバンド（度）。この角度以下の変化は無視")]
    public float deadband = 0.3f;

    [Header("Angle Limits (傾き制限)")]
    [Tooltip("ピッチ角（上下）の制限。±この値を超えない")]
    public float pitchLimit = 45f;
    [Tooltip("ヨー角（左右）の制限。±この値を超えない")]
    public float yawLimit = 60f;

    [Header("Look-Down → Yaw Reset")]
    [Tooltip("下向き判定の閾値（device-forward方向に重力がどれだけ向いてるか）。0.7≈45°、0.5≈60°")]
    [Range(0f, 1f)] public float lookDownThreshold = 0.7f;
    [Tooltip("下を向いた状態を継続する秒数。これだけ続いたらyawを0に")]
    public float lookDownDuration = 0.5f;

    [Header("Keys")]
    public KeyCode keyResetYaw = KeyCode.R; // ヤー（方位）をゼロに
    public KeyCode keyGyroBias = KeyCode.C; // 静止中に押してバイアス再計測

    Quaternion q = Quaternion.identity;       // 推定姿勢（目標）
    Vector3 gyroBias;                         // ジャイロのオフセット（deg/s）
    Vector3 currentEuler;                     // 実際に適用しているオイラー角
    Vector3 dampVel;                          // SmoothDampAngle 内部用
    float lookDownTimer;                      // 下向き継続時間カウンタ
    public Quaternion CurrentOrientation => q;

    void Awake()
    {
        if (Target == null) Target = transform;
    }

    void OnEnable()
    {
        if (autoGyroBiasCalibOnStart) StartCoroutine(CalibGyroBias());
        // 加速度で初期傾きだけ合わせる（yawは未確定→0°）
        if (Source != null)
        {
            var a = MapAxes(Source.Accel, accelSign);
            if (a.sqrMagnitude > 1e-6f) {
                var g = (-a).normalized;           // 重力方向
                q = Quaternion.FromToRotation(Vector3.up, g); // 傾き合わせ（yaw=0）
            }
        }
    }

    IEnumerator CalibGyroBias()
    {
        gyroBias = Vector3.zero;
        float t = 0f;
        int n = 0;
        while (t < biasCalibSeconds)
        {
            if (Source != null)
            {
                gyroBias += MapAxes(Source.Gyro, gyroSign);
                n++;
            }
            yield return null;
            t += Time.deltaTime;
        }
        if (n > 0) gyroBias /= n;
        // ゼロ点取り直し直後は yaw もリセットしたいことが多い
        ResetYaw();
    }

    void Update()
    {
        if (Source == null) return;

        if (Input.GetKeyDown(keyGyroBias)) StartCoroutine(CalibGyroBias());
        if (Input.GetKeyDown(keyResetYaw)) ResetYaw();
        if (Input.GetKeyDown(KeyCode.Z)) {
            q = Quaternion.identity;
            currentEuler = Vector3.zero;
            dampVel = Vector3.zero;
            Target.rotation = Quaternion.identity;
        }
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);

        // --- 1) ジャイロで積分（高速応答）
        Vector3 gyro = MapAxes(Source.Gyro, gyroSign) - gyroBias; // deg/s
        float ang = gyro.magnitude * dt;                           // deg
        if (ang > 0f)
        {
            Quaternion dq = Quaternion.AngleAxis(ang, gyro.normalized);
            // ボディ座標の回転として右積
            q = q * dq;
        }

        // --- 2) 加速度で傾き補正（遅いがドリフトしない、yawは補正できない）
        Vector3 a = MapAxes(Source.Accel, accelSign);
        if (a.sqrMagnitude > 1e-6f)
        {
            Vector3 upPred = q * Vector3.up;   // 予測された世界“上”
            Vector3 upMeas = (-a).normalized;  // 測定した重力方向（=世界“上”）

            Vector3 axis = Vector3.Cross(upPred, upMeas);
            float s = axis.magnitude;
            if (s > 1e-6f)
            {
                float errDeg = Mathf.Asin(Mathf.Clamp(s, -1f, 1f)) * Mathf.Rad2Deg;
                Quaternion corr = Quaternion.AngleAxis(errDeg * accelBlend, axis / s);
                q = corr * q; // 前から補正を掛ける（世界座標の微小修正）
            }

            // 下向き継続でyawリセット（upMeas.z > しきい値 = device-forwardが下向き）
            if (upMeas.z > lookDownThreshold)
            {
                lookDownTimer += dt;
                if (lookDownTimer >= lookDownDuration)
                {
                    ResetYaw();
                    lookDownTimer = 0f;
                }
            }
            else
            {
                lookDownTimer = 0f;
            }
        }
        else
        {
            lookDownTimer = 0f;
        }

        // --- 3) 角度制限（ピッチ・ヨーをclamp、qにも反映してドリフト蓄積を防ぐ）
        Vector3 e = q.eulerAngles;
        float pitch = e.x > 180f ? e.x - 360f : e.x;
        float yaw   = e.y > 180f ? e.y - 360f : e.y;
        pitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);
        yaw   = Mathf.Clamp(yaw,   -yawLimit,   yawLimit);
        e.x = pitch;
        e.y = yaw;
        q = Quaternion.Euler(e);

        // --- 4) 滑らかに適用（SmoothDampAngle）
        Vector3 targetEuler = q.eulerAngles;

        // デッドバンド: 微小な揺れを無視
        if (Mathf.Abs(Mathf.DeltaAngle(currentEuler.x, targetEuler.x)) < deadband)
            targetEuler.x = currentEuler.x;
        if (Mathf.Abs(Mathf.DeltaAngle(currentEuler.y, targetEuler.y)) < deadband)
            targetEuler.y = currentEuler.y;
        if (Mathf.Abs(Mathf.DeltaAngle(currentEuler.z, targetEuler.z)) < deadband)
            targetEuler.z = currentEuler.z;

        // SmoothDampAngle で滑らかに追従
        currentEuler.x = Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref dampVel.x, smoothTime, maxDegPerSec);
        currentEuler.y = Mathf.SmoothDampAngle(currentEuler.y, targetEuler.y, ref dampVel.y, smoothTime, maxDegPerSec);
        currentEuler.z = Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref dampVel.z, smoothTime, maxDegPerSec);

        Target.rotation = Quaternion.Euler(currentEuler);
    }

    void ResetYaw()
    {
        // pitch/roll は保ち、yaw をワールド前方へ合わせる
        // 簡易的に：重力合わせで作り直し（yaw=0）
        var a = MapAxes(Source.Accel, accelSign);
        if (a.sqrMagnitude > 1e-6f)
        {
            var upMeas = (-a).normalized;
            q = Quaternion.FromToRotation(Vector3.up, upMeas);
        }
        // スムージング状態もリセット
        currentEuler = q.eulerAngles;
        dampVel = Vector3.zero;
    }

    Vector3 MapAxes(Vector3 v, Vector3 sign)
    {
        // IMU軸 → Unity軸 の並び替え + 符号反転
        float right   = GetAxisValue(v, imuRightAxis)   * sign.x;
        float up      = GetAxisValue(v, imuUpAxis)      * sign.y;
        float forward = GetAxisValue(v, imuForwardAxis) * sign.z;
        return new Vector3(right, up, forward);
    }

    static float GetAxisValue(Vector3 v, ImuAxis axis)
    {
        switch (axis)
        {
            case ImuAxis.X: return v.x;
            case ImuAxis.Y: return v.y;
            case ImuAxis.Z: return v.z;
            default:        return 0f;
        }
    }
}
