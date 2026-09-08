using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(MeshRenderer))]
public class Target : MonoBehaviour
{
    [SerializeField] private int scoreAmount = 300;
    private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;

    // スコアを表示するためのプレハブ
    [SerializeField] private GameObject scoreTextPrefab;

    // 的から見た表示するスコアの位置
    [SerializeField] private Vector3 offset;

    // 衝突時のエフェクト
    private ParticleSystem ps;

    // 的に割り当てられているMesh Renderer
    private MeshRenderer mr;

    // Mesh Rendererが持つMaterial
    private Material mat;

    private bool hit;
    private GameObject score;
    private TextMeshPro scoreTMPro;

    // 衝突後に透明になる速度
    private float transparentSmoothness = 1.5f;

    private Color finishColor = new Color(0f, 0f ,0f ,0f);

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ps = GetComponent<ParticleSystem>();
        mr = GetComponent<MeshRenderer>();
        mat = mr.material;
    }

    private void Update() {
        // 衝突前ならば実行しない
        if (!hit) return;

        // 衝突後ならだんだん的を透明にしていく
        var smoothedColor = Color.Lerp(mat.color, finishColor, transparentSmoothness * Time.deltaTime);
        mat.SetColor("_BaseColor", smoothedColor);
    }

   void OnDestroy()
   {
        // Rendererで取得したマテリアルはそのままにするとリークする
        Destroy(mat);
   }

   private void OnTriggerEnter(Collider other)
    {
        if (hit) return;
        if (other.gameObject.tag == "Sphere")
        {
            hit = true;
            ScoreManager.Instance.RegisterTargetHit(scoreAmount);
            if (audioSource != null && hitSound != null) audioSource.PlayOneShot(hitSound);
            StartCoroutine(HitPointCenter());
        }
    }

    IEnumerator HitPointCenter()
    {
        foreach (var c in GetComponents<Collider>()) c.enabled = false;
        var mover = GetComponent<TargetMover>();
        if (mover != null) mover.enabled = false;
        
        // スコアを表示
        if (scoreTextPrefab != null)
        {
            score = Instantiate(scoreTextPrefab, transform.position + offset ,Quaternion.identity);
            scoreTMPro = score.GetComponent<TextMeshPro>();
            scoreTMPro.text = $"{scoreAmount}";
        }

        // エフェクトを再生
        if (ps != null)
        {
            ps.Play();
        }

        // 2秒で消えるようにする
        yield return new WaitForSeconds(2);

        Destroy(score);
        Destroy(gameObject);
    }
}
