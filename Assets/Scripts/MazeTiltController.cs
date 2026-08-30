// MazeTiltControllerSmoothDamped.cs
using UnityEngine;

public class MazeTiltController : MonoBehaviour
{
    public PicoMpuClient Source;

    [Header("Tilt limits")]
    public float angleLimitDeg = 16f;

    [Header("Filtering (入力の平滑化)")]
    [Range(0f, 1f)] public float ema = 0.22f;   // 0.15〜0.35 くらい
    public float deadband = 0.15f;             // 小刻みゆれをカット（度）

    [Header("Response (追従の滑らかさ)")]
    public float smoothTime = 0.12f;           // 0.08〜0.18
    public float maxDegPerSec = 80f;           // 1秒あたりの最大傾斜速度

    [Header("Axis")]
    public bool invertX, invertZ;
    public KeyCode calibrateKey = KeyCode.Space;

    float offsetX, offsetZ;
    Vector2 emaTilt;                 // EMA後の目標角(度)
    Vector2 currentTilt;             // 実際に適用している角(度)
    Vector2 dampVel;                 // SmoothDampAngle の角速度(内部用)
    Rigidbody rb;

    [Header("Reset")]
    public KeyCode resetKey = KeyCode.R;

    bool _resetRequested;
    bool _calibRequested;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        CalibrateNow();
        currentTilt = Vector2.zero;
        emaTilt = Vector2.zero;
    }


    void Update()
    {
        if (Input.GetKeyDown(resetKey))    _resetRequested = true;
        if (Input.GetKeyDown(calibrateKey)) _calibRequested = true;
    }
    void FixedUpdate()
    {
        if (_resetRequested) { _resetRequested = false; ResetStageRotation(); }
        if (_calibRequested){ _calibRequested = false; CalibrateNow(); }
        if (Source == null) return;

        // 1) 生の傾き（加速度→角度）
        var a = Vector3.ClampMagnitude(Source.Accel, 1.1f);
        float rawX = Mathf.Rad2Deg * Mathf.Atan2(a.y, a.z);   // pitch
        float rawZ = -Mathf.Rad2Deg * Mathf.Atan2(a.x, a.z);  // roll（符号調整）
        if (invertX) rawX = -rawX;
        if (invertZ) rawZ = -rawZ;
        rawX -= offsetX; rawZ -= offsetZ;

        // 2) クランプ → デッドバンド → EMA
        rawX = Mathf.Clamp(rawX, -angleLimitDeg, angleLimitDeg);
        rawZ = Mathf.Clamp(rawZ, -angleLimitDeg, angleLimitDeg);
        if (Mathf.Abs(rawX - emaTilt.x) < deadband) rawX = emaTilt.x;
        if (Mathf.Abs(rawZ - emaTilt.y) < deadband) rawZ = emaTilt.y;
        emaTilt.x = Mathf.Lerp(emaTilt.x, rawX, ema);
        emaTilt.y = Mathf.Lerp(emaTilt.y, rawZ, ema);

        // 3) 角速度を制限しつつ滑らかに目標へ（スッと追従）
        float stepMax = maxDegPerSec; // SmoothDampAngle は maxSpeed を度/秒で受ける
        currentTilt.x = Mathf.SmoothDampAngle(currentTilt.x, emaTilt.x, ref dampVel.x, smoothTime, stepMax, Time.fixedDeltaTime);
        currentTilt.y = Mathf.SmoothDampAngle(currentTilt.y, emaTilt.y, ref dampVel.y, smoothTime, stepMax, Time.fixedDeltaTime);

        var q = Quaternion.Euler(currentTilt.x, 0f, currentTilt.y);
        if (rb && rb.isKinematic) rb.MoveRotation(q);
        else transform.localRotation = q;

        if (Input.GetKeyDown(calibrateKey)) CalibrateNow();
    }

    public void CalibrateNow()
    {
        if (Source == null) { offsetX = offsetZ = 0f; return; }
        var a = Source.Accel;
        float x = Mathf.Rad2Deg * Mathf.Atan2(a.y, a.z);
        float z = -Mathf.Rad2Deg * Mathf.Atan2(a.x, a.z);
        if (invertX) x = -x; if (invertZ) z = -z;
        offsetX = x; offsetZ = z;
        emaTilt = currentTilt = Vector2.zero;
        if (rb && rb.isKinematic) rb.MoveRotation(Quaternion.identity);
        else transform.localRotation = Quaternion.identity;
    }
    
    public void ResetStageRotation()
    {
        // 今の傾きを「ゼロ」として再定義
        CalibrateNow();

        // スムージング状態をリセット（あれば）
        // これらの変数が無い版はこの3行は削ってOK
        // emaTilt/currentTilt/dampVel は例の SmoothDamped 版に存在します
        try {
            emaTilt = Vector2.zero;
            currentTilt = Vector2.zero;
            dampVel = Vector2.zero;
        } catch {}

        // 物理と同期して 0,0,0 に
        var q = Quaternion.identity;
        if (rb != null && rb.isKinematic) rb.MoveRotation(q);
        else transform.localRotation = q;
    }

}
