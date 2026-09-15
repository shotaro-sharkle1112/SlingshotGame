using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Time Limit")]
    [SerializeField] private float timeLimit = 60f;
    [Header("BGM")]
    [SerializeField] private AudioSource resultAudioSource;
    [SerializeField] private AudioClip finishJingle;
    [SerializeField] private AudioSource bgmAudioSource;

    // 外部のクラスから確認できるゲーム状態の数値

    public enum GameState
    {
        // ゲームが始まる前
        BeforePlaying,
        // プレイ中
        Playing,
        // プレイ終了後(リザルト表示など)
        AfterPlaying
    }

    public GameState gameState {get; private set;}

    public float remainingTime {get; private set;}
    public bool timerRunning {get; private set;}
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //タイムをセットして、タイマー開始
        remainingTime = timeLimit;
        timerRunning = true;
        // ゲームプレイ中にする(現段階ではBeforePlayingで行う処理がないため最初からPlaying)
        gameState = GameState.Playing;

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
        // プレイを終了する状態に変更
        gameState = GameState.AfterPlaying;
    }

    private void StopBgm()
    {
        if (bgmAudioSource != null) bgmAudioSource.Stop();
    }

    private void PlayJingle()
    {
        if (resultAudioSource == null || finishJingle == null) return;
        resultAudioSource.PlayOneShot(finishJingle);
    }
}
