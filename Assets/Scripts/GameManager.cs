using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Time Limit")]
    [SerializeField] private float timeLimit = 60f;
    [Header("BGM")]
    [SerializeField] private AudioSource resultAudioSource;
    [SerializeField] private AudioClip resultJingle;
    [SerializeField] private AudioSource bgmAudioSource;

    public float remainingTime {get; private set;}
    public bool timerRunning {get; private set;}
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //タイムをセットして、タイマー開始
        remainingTime = timeLimit;
        timerRunning = true;

        if (bgmAudioSource != null && !bgmAudioSource.isPlaying) bgmAudioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        if (!timerRunning) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            timerRunning = false;
            OnTimeUp();
        }
    }

    private void OnTimeUp()
    {
        Debug.Log("Time up!");
        StopBgm();
        PlayJingle();
        Time.timeScale = 0f; // ゲーム停止：Time.deltaTimeが0になり、的の動き等が止まる
    }

    private void StopBgm()
    {
        if (bgmAudioSource != null) bgmAudioSource.Stop();
    }

    private void PlayJingle()
    {
        if (resultAudioSource == null || resultJingle == null) return;
        resultAudioSource.PlayOneShot(resultJingle);
    }
}
