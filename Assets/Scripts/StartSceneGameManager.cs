using UnityEngine;
using System.Collections;
using UnityEditor.SearchService;
using UnityEngine.SceneManagement;

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
    public bool IsStart => isStart;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (bgmAudioSource != null && !bgmAudioSource.isPlaying) bgmAudioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        // シーン切り替えのトリガーが入ったら音量を小さくして、早期リターン
        if (isStart)
        {
            bgmAudioSource.volume = Mathf.Lerp(bgmAudioSource.volume, 0f, Time.deltaTime);
            return;
        }

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

            // シーン切り替えコルーチンを実施
            StartCoroutine(NextScene());
        } 

    }

    IEnumerator NextScene()
    {
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene("TutorialSlingshotGame");
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
