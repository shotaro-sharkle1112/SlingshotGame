using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class MetalSphere : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.MainModule mainps;
    private ParticleSystem.EmissionModule emissionps;
    [SerializeField] private Color chargeColor;

    // 同時に当たった的の数
    private int hitCount;

    // チャージショットかどうか
    public bool isChargeShot;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();

        mainps = ps.main;
        emissionps = ps.emission;
        // 一旦停止させる
        ps.Pause();

        // 変数のリセット
        hitCount = 0;
        isChargeShot = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   void OnDestroy()
   {
      Destroy(ps);
   }

   // その球が発射されたら実行される
   // エフェクトの再生に利用します
   public void EnableParticleEffect()
    {
        ps.Play();
    } 

    // チャージ用のエフェクト
    public void EnableChargeShotParticleEffect()
    {
        mainps.startColor = chargeColor;
        emissionps.rateOverTime = 100;
        ps.Play();
    } 

    // 的に衝突した回数の公開API
    public int GetHitCount() { return hitCount; }
    public void AddHitCount() { hitCount += 1; }

    // Slingshot.csから呼び出される自己破壊の予約関数
    public IEnumerator destroyAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(gameObject);
    }
}
