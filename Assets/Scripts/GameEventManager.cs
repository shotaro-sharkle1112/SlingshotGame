using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class GameEventManager : MonoBehaviour
{

    [Header("プレイヤー監視設定")]
    [Tooltip("監視したいプレイヤーのTransform")]
    public Transform playerTransform;

    [Tooltip("プレイヤーのRigidbody（速度リセット用、なければ空でOK）")]
    public Rigidbody playerRigidbody;

    [Tooltip("範囲を外れたとき戻す座標")]
    public Vector3 respawnPosition = new Vector3(5f, 2f, -4f);

    [Header("許容範囲（境界）")]
    public float minX = -7f;
    public float maxX = 8f;
    public float minZ = -6f;
    public float maxZ = 6f;

    [Header("カメラ移動設定")]
    [Tooltip("スコア1あたり下がる高さ（正の値）。実際は下向きに動くので符号は内部で反映します。")]
    public float heightPerScore = 2f;

    [Tooltip("スムーズに移動するか（falseだと瞬間移動）")]
    public bool smoothMove = true;

    [Tooltip("スムーズ移動の速度（units/sec）")]
    public float moveSpeed = 5f;

    [Tooltip("カメラのTransform（Main Cameraをドラッグ）")]
    public Transform cameraTransform;

    private Vector3 camBasePos;
    private int lastScore;

    [Header("カウントダウン設定")]
    [Tooltip("カウントダウンする秒数")]
    public float startTime = 30f;

    [Tooltip("表示するUI Text(残り時間)")]
    public TextMeshProUGUI countdownText;
    [Tooltip("表示するUI Text(スコア)")]
    public TextMeshProUGUI scoreText;

    [Header("UI参照設定")]
    [Tooltip("スタートボタン")]
    public Button startButton;

    [Tooltip("名前入力フィールド")]
    public TMP_InputField nameInput;

    [Tooltip("Finish画面に結果を表示するテキスト（例：\"Alice 123点\"）")]
    public TextMeshProUGUI resultText;

    [Tooltip("Finish画面にランキングを表示するテキスト（任意）")]
    public TextMeshProUGUI leaderboardText;
    [Tooltip("流す曲")]
    public AudioClip sound1;
    [Tooltip("終了の効果音")]
    public AudioClip sound2;
    AudioSource audioSource;

    private GameObject startFrame;
    private GameObject finishFrame;

    private float timeLeft;
    private bool isStart = false;
    private bool savedOnFinish = false;

    // 要素数5のVector3配列を宣言
    private Vector3[] respawnPoints = new Vector3[5];

    private bool finalGoal = false;


    // ゲーム中に更新していくスコア（他スクリプトから加点してOK）
    [Header("ゲームスコア")]
    public int currentScore = 0;

    public void AddScore(int amount)
    {
        currentScore += amount;
        Debug.Log($"スコア加算: +{amount} → 現在 {currentScore}");
    }

    // ====== Leaderboard 用の簡易クラス ======
    [Serializable]
    public class ScoreEntry
    {
        public string name;
        public int score;
        public string date; // 保存日時（表示用）
    }

    [Serializable]
    public class ScoreList
    {
        public List<ScoreEntry> entries = new List<ScoreEntry>();
    }

    void Start()
    {

        audioSource = GetComponent<AudioSource>();
        // 各要素に代入
        respawnPoints[0] = new Vector3(5f, 4f - heightPerScore*0, -4f);
        respawnPoints[1] = new Vector3(5f, 4f - heightPerScore*1, 3f);
        respawnPoints[2] = new Vector3(-5f, 4f - heightPerScore*2, -4f);
        respawnPoints[3] = new Vector3(5f, 4f - heightPerScore*3, -4f);
        respawnPoints[4] = new Vector3(5f, 4f - heightPerScore*4, -4f);


        if (cameraTransform == null)
        {
            // 未指定ならMain Cameraを自動補完（なければ警告）
            Camera cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
            else Debug.LogWarning("cameraTransform が未設定です。Main Camera も見つかりません。");
        }

        if (cameraTransform != null)
        {
            camBasePos = cameraTransform.position; // X/Zは固定、Yだけ可変
        }
        lastScore = currentScore;

        timeLeft = startTime;

        startFrame  = GameObject.Find("Start Frame");
        finishFrame = GameObject.Find("Finish Frame");
        if (finishFrame != null) finishFrame.SetActive(false);

        if (startButton != null)
            startButton.onClick.AddListener(OnButtonClicked);

        // 直前の名前が保存済みなら復元（任意）
        if (PlayerPrefs.HasKey("PlayerName") && nameInput != null)
            nameInput.text = PlayerPrefs.GetString("PlayerName");
    }

    void Update()
    {
        // プレイヤーの位置を監視
        if (playerTransform != null)
        {
            Vector3 pos = playerTransform.position;

            // XとZが指定範囲を外れたら戻す
            if (pos.x < minX || pos.x > maxX || pos.z < minZ || pos.z > maxZ || -4f - heightPerScore*currentScore > playerTransform.position.y)
            {
                Debug.Log("プレイヤーが範囲外に出たためリスポーンします。");
                RespawnPlayer();
            }
        }

        // カメラの移動
        if (cameraTransform != null)
        {
            float targetY = camBasePos.y - currentScore * heightPerScore;
            Vector3 targetPos = new Vector3(camBasePos.x, targetY, camBasePos.z);

            if (smoothMove)
            {
                cameraTransform.position =
                    Vector3.MoveTowards(cameraTransform.position, targetPos, moveSpeed * Time.deltaTime);
            }
            else
            {
                // 瞬間移動はスコア変更時だけ更新してもOK
                if (currentScore != lastScore)
                    cameraTransform.position = targetPos;
            }
        }

        lastScore = currentScore;

        // スコアの更新
        if (scoreText != null) scoreText.text = currentScore.ToString();

        if (isStart && timeLeft > 0f)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft < 0f) timeLeft = 0f;

            int seconds = Mathf.CeilToInt(timeLeft);
            if (countdownText != null) countdownText.text = seconds.ToString();
        }

        // タイムアップ時の処理（1回だけ）
        if ((isStart && timeLeft == 0f && !savedOnFinish)||(isStart && finalGoal))
        {
            savedOnFinish = true;
            OnTimeUp();
        }
    }

    private void RespawnPlayer()
    {
        // 位置をリセット
        playerTransform.position = respawnPoints[currentScore];

        // 速度もリセット（Rigidbodyがある場合）
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }

    void OnButtonClicked()
    {
        // 名前を確定
        string playerName = GetEnteredName();
        PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.Save();

        // スタートUIを閉じてゲーム開始
        if (startFrame != null) startFrame.SetActive(false);
        isStart = true;

        audioSource.PlayOneShot(sound1);
    }


    public void gameFinish()
    {
        finalGoal = true;
    }

    void OnTimeUp()
    {

        audioSource.Stop();
        audioSource.PlayOneShot(sound2);
        // 名前の取得（空なら "Player"）
        string playerName = GetEnteredName();

        // 保存：最後のスコア
        PlayerPrefs.SetInt("LastScore", currentScore);

        // 保存：ベストスコア
        int best = PlayerPrefs.GetInt("BestScore", int.MinValue);
        if (currentScore > best)
        {
            PlayerPrefs.SetInt("BestScore", currentScore);
            best = currentScore;
        }

        // 保存：名前
        PlayerPrefs.SetString("PlayerName", playerName);

        // オプション：簡易リーダーボードに追記（上位5件保持）
        AddToLeaderboard(playerName, currentScore);

        PlayerPrefs.Save();

        // Finish 画面を表示 & 文言更新
        if (finishFrame != null) finishFrame.SetActive(true);

        if (resultText != null)
        {
            resultText.text = $"プレイヤー: {playerName}\n点数: {currentScore}\nBest: {best}";
        }

        if (leaderboardText != null)
        {
            leaderboardText.text = BuildLeaderboardText(5);
        }

        // 必要ならゲームの進行を止める
        // Time.timeScale = 0f;
    }

    string GetEnteredName()
    {
        string entered = nameInput != null ? nameInput.text?.Trim() : "";
        return string.IsNullOrEmpty(entered) ? "Player" : entered;
    }

    // ====== Leaderboard 保存/表示（任意） ======
    void AddToLeaderboard(string name, int score)
    {
        ScoreList list = LoadLeaderboard();
        list.entries.Add(new ScoreEntry {
            name = name,
            score = score,
            date = DateTime.Now.ToString("yyyy/MM/dd HH:mm")
        });

        // スコア降順に並べ替え、上位5件だけを保存
        list.entries.Sort((a, b) => b.score.CompareTo(a.score));
        if (list.entries.Count > 5) list.entries = list.entries.GetRange(0, 5);

        SaveLeaderboard(list);
    }

    ScoreList LoadLeaderboard()
    {
        string json = PlayerPrefs.GetString("Leaderboard", "");
        if (string.IsNullOrEmpty(json))
            return new ScoreList();

        try
        {
            return JsonUtility.FromJson<ScoreList>(json) ?? new ScoreList();
        }
        catch
        {
            // 破損時は初期化
            return new ScoreList();
        }
    }

    void SaveLeaderboard(ScoreList list)
    {
        string json = JsonUtility.ToJson(list);
        PlayerPrefs.SetString("Leaderboard", json);
    }

    string BuildLeaderboardText(int topN)
    {
        ScoreList list = LoadLeaderboard();
        int n = Mathf.Min(topN, list.entries.Count);
        if (n == 0) return "No records yet.";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < n; i++)
        {
            var e = list.entries[i];
            sb.AppendLine($"{i + 1}. {e.name}  {e.score}  ({e.date})");
        }
        return sb.ToString();
    }
}
