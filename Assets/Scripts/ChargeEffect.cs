using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ChargeEffect : MonoBehaviour
{


    // チャージ中と、チャージ完了でエフェクトを再生したい
    [SerializeField] private Slingshot slingshot;

    // エフェクト用のparticle system
    private ParticleSystem m_particleSystem;
    // 色を変える用のmainモジュール
    private ParticleSystem.MainModule mainModule;

    // 一フレーム前のパチンコの状態
    private Slingshot.SlingshotState m_slingshotState;
    
    void Start()
    {
        m_particleSystem = GetComponent<ParticleSystem>();
        mainModule = m_particleSystem.main;
        // いったん止めておく
        m_particleSystem.Stop();

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
                Debug.Log("effect stop");
                m_particleSystem.Stop();
            }else
            {
                Debug.Log("effect start");
                m_particleSystem.Play();
            }
        }

        // 新しい状態に書き換え
        m_slingshotState = slingshot.slingshotState;
    }
}
