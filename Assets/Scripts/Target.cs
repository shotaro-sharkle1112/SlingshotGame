using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Rigidbody))]
public class Target : MonoBehaviour
{
    [SerializeField] private int scoreAmount = 300;
    private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;

    // スコアを表示するためのプレハブ
    [SerializeField] private GameObject scoreTextPrefab;

    // 的から見た表示するスコアの位置
    [SerializeField] private Vector3 offset;
    [SerializeField] private float hitPower = 5f;
    [SerializeField] private float vanishTime = 4f;

    // 衝突時のエフェクト
    private ParticleSystem ps;

    // 的に割り当てられているMesh Renderer
    private MeshRenderer mr;

    // Mesh Rendererが持つMaterial
    private Material mat;

    // 当たった時に重力をオンにしたり、吹っ飛ばすためのrigidbody
    private Rigidbody rigidbody;

    private bool hit;
    private GameObject score;
    private TextMeshPro scoreTMPro;

    private GameObject hitCountText;
    private TextMeshPro hitCountTextTMPro;

    // 衝突後に透明になる速度
    private float transparentSmoothness = 1.5f;

    private Color finishColor = new Color(0f, 0f ,0f ,0f);

    // マトが出現するときに小さい状態から拡大していくアニメーションの数値
    private float appearAnimationElapesd;
    // マトの大きさを保持しておく変数
    private Vector3 sizeScale;

    // 最終的にアニメーションがたどり着く倍率
    public float settingScale = 1f;

   void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ps = GetComponent<ParticleSystem>();
        mr = GetComponent<MeshRenderer>();
        mat = mr.material;
        rigidbody = GetComponent<Rigidbody>();

        appearAnimationElapesd = 0.01f;
        sizeScale = transform.localScale;
        transform.localScale *= appearAnimationElapesd;
        
    }

    private void Update() {

        if (appearAnimationElapesd <= settingScale)
        {
            transform.localScale = sizeScale * appearAnimationElapesd;
            appearAnimationElapesd += Time.deltaTime * 3f;
            Debug.Log(appearAnimationElapesd);
        }
        else
        {
            transform.localScale = sizeScale * settingScale;
        }
        // 衝突前ならば早期リターン
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

            // 弾のヒット回数をカウント
            var metalSphereScript = other.gameObject.GetComponent<MetalSphere>();
            metalSphereScript.AddHitCount();

            // その的のスコア x その弾のヒット回数
            if (ScoreManager.Instance != null) ScoreManager.Instance.RegisterTargetHit(metalSphereScript.GetHitCount() * scoreAmount);
            if (audioSource != null && hitSound != null) audioSource.PlayOneShot(hitSound);
            
            // 衝突時に重力をオンにして少し吹っ飛ばすことで爽快感を増やす
            rigidbody.useGravity = true;
            rigidbody.AddForce(rigidbody.mass * hitPower * Vector3.forward,ForceMode.Impulse);

            // マトの消滅を予約
            StartCoroutine(HitPointCenter(metalSphereScript.GetHitCount()));
        }
    }
    IEnumerator HitPointCenter(int hitCount)
    {
        foreach (var c in GetComponents<Collider>()) c.enabled = false;
        var mover = GetComponent<TargetMover>();
        if (mover != null) mover.enabled = false;
        
        // スコアを表示するときに、カメラ目線から見てそのマトの近くに描画しないといけない
        var targetPosition = transform.position;
        var ratio = 50f / (targetPosition.magnitude * Mathf.Abs(Vector3.Dot(targetPosition.normalized, Vector3.forward)));
        // スコアを表示
        if (scoreTextPrefab != null)
        {
            score = Instantiate(scoreTextPrefab, 10f * new Vector3(Random.Range(0.5f, 1f), Random.Range(0.5f, 1f), 0f) + new Vector3(transform.position.x * ratio, transform.position.y * ratio, 50f - hitCount) + offset, Quaternion.identity);
            scoreTMPro = score.GetComponent<TextMeshPro>();
            scoreTMPro.text = hitCount >= 2 ? $"{hitCount} x {scoreAmount}" : $"{scoreAmount}";
            scoreTMPro.fontSize = 30f + hitCount * 2f;
            scoreTMPro.outlineColor = Color.cyan;
            scoreTMPro.outlineWidth = 0.2f;
        }

        // ヒット数を表示
        if (scoreTextPrefab != null && hitCount >= 2)
        {
            hitCountText = Instantiate(scoreTextPrefab, 10f * new Vector3(Random.Range(0.5f, 1f), Random.Range(0.5f, 1f), 0f) +  new Vector3(transform.position.x * ratio, transform.position.y * ratio, - hitCount)  + offset + new Vector3(5f, 4f, 50f), Quaternion.identity);
            hitCountTextTMPro = hitCountText.GetComponent<TextMeshPro>();
            hitCountTextTMPro.text = $"{hitCount} Combo !!";
            hitCountTextTMPro.color = Color.yellow;
            hitCountTextTMPro.fontSize = 40f + hitCount * 2f;
            hitCountTextTMPro.outlineColor = Color.black;
            hitCountTextTMPro.outlineWidth = 0.5f;
        }

        // エフェクトを再生
        if (ps != null)
        {
            ps.Play();
        }

        // 2秒で消えるようにする
        yield return new WaitForSeconds(vanishTime);

        Destroy(score);
        Destroy(hitCountText);
        Destroy(gameObject);
    }
}
