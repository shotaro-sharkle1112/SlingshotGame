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

    [Header("BGM")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip startJingle;
    [SerializeField] private AudioSource bgmAudioSource;

    // チャージしている時間
    private float chargingTime = 0f;
    // chargingTime / chargingTimeThreshold = chargingRatio
    private float chargingRatio = 0f;

    public float ChargingRatio => chargingRatio;

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
            
        }
        else
        {
            // 曲げていないのでチャージ時間を削る
            chargingTime = Mathf.Max(0f, chargingTime - Time.deltaTime);
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


    private void PlayJingle()
    {
        if (audioSource != null && startJingle != null)
        {
            audioSource.PlayOneShot(startJingle);
        }else
        {
            Debug.Log("null : cannot play jingle");
        }
    }
}
