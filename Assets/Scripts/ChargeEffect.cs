using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class ChargeEffect : MonoBehaviour
{


    // チャージ中と、チャージ完了でエフェクトを再生したい
    [SerializeField] private Slingshot slingshot;

    // チャージ完了演出のエフェクト
    [SerializeField] private ParticleSystem m_particleSystemChargeCompleted;
    // エフェクト用のparticle system
    private ParticleSystem m_particleSystem;
    // 色を変える用のmainモジュール
    private ParticleSystem.MainModule mainModule;

    // 一フレーム前のパチンコの状態
    private Slingshot.SlingshotState m_slingshotState;

   void OnEnable()
   {
      if (slingshot != null) slingshot.OnChargeCompleted += Flash;
   }

   void OnDisable()
   {
      if (slingshot != null) slingshot.OnChargeCompleted -= Flash;
   }
   void Start()
    {
        m_particleSystem = GetComponent<ParticleSystem>();
        mainModule = m_particleSystem.main;
        // いったん止めておく
        m_particleSystem.Stop();
        m_particleSystemChargeCompleted.Stop();

        m_slingshotState = Slingshot.SlingshotState.Idle;
    }

    // Update is called once per frame
    void Update()
    {
        var chargeRatio = slingshot.chargeRatio;

        // チャージの割合でエフェクトの色を変える
        mainModule.startColor = new ParticleSystem.MinMaxGradient(new Color(1.0f, 1.0f - chargeRatio, 0.0f));
        
        
        // もしパチンコのチャージの変化があればエフェクトを適宜再生停止する
        if (slingshot.slingshotState != m_slingshotState)
        {
            Debug.Log(chargeRatio);
            if (chargeRatio == 0f)
            {
                m_particleSystem.Stop();
            }else
            {
                //m_particleSystem.Play();
            }
        }

        // 新しい状態に書き換え
        m_slingshotState = slingshot.slingshotState;
    }

    void Flash()
    {
        m_particleSystemChargeCompleted.Play();
        StartCoroutine(StopParticleSystem());
    }

    IEnumerator StopParticleSystem()
    {
        yield return new WaitForSeconds(1f);
        m_particleSystemChargeCompleted.Stop();
    }
}
