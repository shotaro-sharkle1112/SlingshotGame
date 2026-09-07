using UnityEngine;

public class StartSceneGameManager : MonoBehaviour
{
    
    [Header("Controler Manager")]
    [SerializeField] private ControlerManager Source;

    [Header("Start Threshold")]
    // 曲げ続けて、何秒でシーンスタートするか
    [SerializeField] private float chargingTimeThreshold = 5f;

    [Header("Bend Threshold")]
    // 曲げセンサ値がどれくらいで曲げ判定にするか
    [SerializeField] private int bendThreshold = 19000;

    [Header("Sound")]
    [SerializeField] private AudioSource jingleAudioSource;
    [SerializeField] private AudioClip startJingle;
    [SerializeField] private AudioSource chargeAudioSource;
    [SerializeField] private AudioClip chargeJingle;
    [SerializeField] private AudioSource bgmAudioSource;

    // チャージしている時間
    private float chargingTime = 0f;
    // chargingTime / chargingTimeThreshold = chargingRatio
    private float chargingRatio = 0f;

    public float ChargingRatio => chargingRatio;
    // スタート演出はしたかどうか
    private bool isStart = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (bgmAudioSource != null && !bgmAudioSource.isPlaying) bgmAudioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        // もし曲げているのであればチャージ時間を計測
        if (bendThreshold > Source.Bend)
        {
            chargingTime = Mathf.Min(chargingTimeThreshold, chargingTime + Time.deltaTime);
            // 一回もチャージ中に再生していなければチャージ音を鳴らす
            if (!chargeAudioSource.isPlaying && !isStart) chargeAudioSource.PlayOneShot(chargeJingle);
        }
        else
        {
            // 曲げていないのでチャージ時間を削る
            chargingTime = Mathf.Max(0f, chargingTime - Time.deltaTime);
            // 曲げていないのでチャージオンを止める
            if (chargeAudioSource.isPlaying) chargeAudioSource.Stop();
        }
   

        if (chargingTimeThreshold > 0f)
        {
            chargingRatio = chargingTime / chargingTimeThreshold;
        }
        else
        {
            // 閾値が0以下なので警告
            Debug.Log($"chargingTimeThreshold is less than 0! : {chargingTimeThreshold}");
        }
        
        // チャージが貯まったらスタート演出
        if (chargingRatio >= 0.999f && !isStart)
        {
            isStart = true;
            PlayJingle(startJingle);
            // チャージが完了したのでストップ
            if (chargeAudioSource.isPlaying) chargeAudioSource.Stop();
        } 

    }

    private void StopBgm()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
        }else
        {
            Debug.Log("null : cannot play bgm");
        }
    }


    private void PlayJingle(AudioClip clip)
    {
        if (jingleAudioSource != null && clip != null)
        {
            jingleAudioSource.PlayOneShot(clip);
        }else
        {
            Debug.Log("null : cannot play jingle");
        }
    }
}
