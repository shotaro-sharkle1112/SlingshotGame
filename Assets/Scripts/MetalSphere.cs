using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class MetalSphere : MonoBehaviour
{
    private ParticleSystem ps;
    private ParticleSystem.MainModule mainps;
    private ParticleSystem.EmissionModule emissionps;

    // metalsphereが一定時間過ぎたら削除
    private float lifetime;
    [SerializeField] private Color chargeColor;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ps = GetComponent<ParticleSystem>();

        mainps = ps.main;
        emissionps = ps.emission;
        // 一旦停止させる
        ps.Pause();

        lifetime = 0.0f;
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

    public IEnumerator destroyAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(gameObject);
    }
}
