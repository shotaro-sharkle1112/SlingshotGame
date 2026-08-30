// AccelMoveXZ.cs
// IMUの加速度(g)を X/Z の移動速度にマッピングしてキューブを動かす
using UnityEngine;

public class AccelMoveXZ : MonoBehaviour
{
    public PicoMpuClient Source;     // Accel(g) を持っているコンポーネント
    [Header("Move")]
    public float speedPerG = 3.0f;   // 1g 相当の傾き時の移動速度[m/s]（傾きは0〜0.5g程度）
    public float maxSpeed = 5.0f;    // 速度上限[m/s]
    public bool useRigidbody = true; // Rigidbodyがあるなら MovePosition 推奨

    [Header("Smoothing / Calibration")]
    [Range(0f,1f)] public float smooth = 0.25f; // 0=なし, 1=瞬時（0.15〜0.35で調整）
    public float deadband = 0.03f;   // g単位のデッドバンド（小刻みな揺れを無視）
    public bool invertX = false;     // 必要なら軸反転
    public bool invertZ = false;
    public KeyCode calibrateKey = KeyCode.Space; // その場をニュートラルに

    Rigidbody rb;
    Vector2 ema;          // 平滑化後の入力（X,Z）
    Vector2 offset;       // ニュートラルオフセット（キャリブ用）

    void Awake() { rb = GetComponent<Rigidbody>(); }
    void Start() { CalibrateNow(); }

    void Update()
    {
        if (Input.GetKeyDown(calibrateKey)) CalibrateNow();
    }

    void FixedUpdate()
    {
        if (Source == null) return;

        // Accel は g 単位。ここでは X→左右、Y→前後 として扱い、Y軸の移動は無視
        // （前に作った迷路のコードと同じ想定: ax=右(+), ay=前(+), az=上(+)）
        Vector3 a = Source.Accel; // g

        // ニュートラル補正＆反転
        float ix = invertX ? -1f : 1f;
        float iz = invertZ ? -1f : 1f;
        Vector2 raw = new Vector2(a.x * ix, a.y * iz) - offset;

        // デッドバンド
        if (raw.magnitude < deadband) raw = Vector2.zero;

        // EMAで平滑化
        ema = Vector2.Lerp(ema, raw, Mathf.Clamp01(smooth));

        // 速度にマッピング（XZのみ）
        Vector3 vel = new Vector3(ema.x, 0f, ema.y) * speedPerG;
        if (vel.magnitude > maxSpeed) vel = vel.normalized * maxSpeed;

        // 位置適用
        float dt = Time.fixedDeltaTime;
        Vector3 dp = vel * dt;

        if (useRigidbody && rb != null) rb.MovePosition(rb.position + dp);
        else transform.position += dp;
    }

    [ContextMenu("Calibrate Now")]
    public void CalibrateNow()
    {
        // 現在の傾きを“ゼロ入力”として記憶（机上でやると良い）
        if (Source == null) { offset = Vector2.zero; return; }
        Vector3 a = Source.Accel;
        float ix = invertX ? -1f : 1f;
        float iz = invertZ ? -1f : 1f;
        offset = new Vector2(a.x * ix, a.y * iz);
        ema = Vector2.zero;
    }
}
