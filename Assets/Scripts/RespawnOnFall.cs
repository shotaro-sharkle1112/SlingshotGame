using UnityEngine;

public class RespawnOnFall : MonoBehaviour
{
    public GameEventManager manager;

    private Rigidbody rb;

    public AudioClip sound1;
    AudioSource audioSource;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start () {
        //Componentを取得
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Goal"))
        {
            audioSource.PlayOneShot(sound1);
            // スコア加算
            if (manager != null)
            {
                manager.AddScore(1);
            }
            else
            {
                Debug.LogWarning("GameEventManagerの参照が設定されていません！");
            }

            // ★ X/Z 方向の速度のみゼロにする（Unity6以降は linearVelocity を使用）
            if (rb != null)
            {
                Vector3 v = rb.linearVelocity;  // ← velocity → linearVelocity に変更
                v.x = 0f;                        // X方向を停止
                v.z = 0f;                        // Z方向を停止
                rb.linearVelocity = v;

                // 回転速度（angularVelocity）はそのままでもOK
                // 必要なら同様にX/Zのみゼロにする
                Vector3 av = rb.angularVelocity;
                av.x = 0f;
                av.z = 0f;
                rb.angularVelocity = av;
            }
        }

        if (other.CompareTag("Final"))
        {
            audioSource.PlayOneShot(sound1);
            // スコア加算
            if (manager != null)
            {
                manager.AddScore(1);
            }
            manager.gameFinish();
        }
    }
}
