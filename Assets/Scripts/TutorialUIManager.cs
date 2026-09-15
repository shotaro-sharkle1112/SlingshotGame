using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialUIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Result")]
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private GameObject resultBackground;
    [SerializeField] private GameObject resetButton;

    [Header("GameManager")]
    [SerializeField] private GameManager gameManager;
    private bool timerRunning;

    void Start()
    {
        UpdateScoreText(ScoreManager.Instance.CurrentScore);
        UpdateHighScoreText(ScoreManager.Instance.HighScore);

        ScoreManager.Instance.OnScoreChanged += UpdateScoreText;
        ScoreManager.Instance.OnHighScoreChanged += UpdateHighScoreText;

        UpdateTimeText(gameManager.remainingTime);

        timerRunning = true;

        if (resultText != null) resultText.gameObject.SetActive(false);
        if (resultBackground != null) resultBackground.SetActive(false);
        if (resetButton != null) resetButton.SetActive(false);
    }

    void Update()
    {
        if (!timerRunning) return;
        if (gameManager.remainingTime <= 0f)
        {
            ShowResult();
            timerRunning = false;
        }
        UpdateTimeText(gameManager.remainingTime);
    }

   private void OnDestroy()
   {
      if (ScoreManager.Instance == null) return;

      ScoreManager.Instance.OnScoreChanged -= UpdateScoreText;
      ScoreManager.Instance.OnHighScoreChanged -= UpdateHighScoreText;
   }

   private void UpdateScoreText(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {score}";
        }
    }

    private void UpdateHighScoreText(int highScore)
    {
        highScoreText.text = $"HIGH SCORE: {highScore}";
    }

    private void UpdateTimeText(float seconds)
    {
        if (timeText == null) return;
        int total = Mathf.CeilToInt(seconds);
        int mm = total / 60;
        int ss = total % 60;
        timeText.text = $"{mm:00}:{ss:00}";
    }

    private void ShowResult()
    {
        if (resultBackground != null) resultBackground.SetActive(true);
        if (resetButton != null) resetButton.SetActive(true);
        if (resultText == null) return;

        var gm = ScoreManager.Instance;
        int c100 = gm.GetHitCount(100);
        int c300 = gm.GetHitCount(300);
        int c500 = gm.GetHitCount(500);
        int c1000 = gm.GetHitCount(1000);
        int total = gm.CurrentScore;

        resultText.text =
            "=== RESULT ===\n" +
            $"100pt  x {c100} = {100 * c100}\n" +
            $"300pt  x {c300} = {300 * c300}\n" +
            $"500pt  x {c500} = {500 * c500}\n" +
            $"1000pt x {c1000} = {1000 * c1000}\n" +
            "──────────────\n" +
            $"TOTAL: {total}";

        resultText.gameObject.SetActive(true);
    }

    // リセットボタンの onClick から呼ぶ。timeScaleを戻してスコアもクリアしてチュートリアルへ。
    public void OnResetButtonClicked()
    {
        Time.timeScale = 1f;
        ScoreManager.Instance.ResetScore();
        SceneManager.LoadScene("StartScene");
    }
}

