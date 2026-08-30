using UnityEngine;

public class ZAxisPingPong : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("中心位置")]
    public float centerZ = 0f;

    [Tooltip("中心からの振幅（±方向）")]
    public float amplitude = 5f;

    [Tooltip("移動速度（単位: 1秒あたりの往復速度）")]
    public float speed = 1f;

    private float startZ;

    void Start()
    {
        // 初期のZ位置を保存（オブジェクトの初期位置を中心にしたい場合）
        startZ = transform.position.z;
    }

    void Update()
    {
        // Mathf.PingPongで0〜amplitude*2を往復させる
        float offset = Mathf.PingPong(Time.time * speed, amplitude * 2) - amplitude;

        // XとYは固定、Zだけ更新
        Vector3 pos = transform.position;
        pos.z = startZ + offset;
        transform.position = pos;
    }
}
