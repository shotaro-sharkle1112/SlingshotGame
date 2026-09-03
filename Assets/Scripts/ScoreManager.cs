using UnityEngine;
using System;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    // 自身をシングルトン化して、外部に公開
    public static ScoreManager Instance {get; private set;}

    // 現在プレイしているステージのスコア
    public int CurrentScore { get; private set;}
    // そのステージのスコア
    public int HighScore { get; private set;}

    // 外部に公開する用に読み込み専用の的ヒット回数の変数
    public IReadOnlyDictionary<int, int> HitCounts => hitCounts;
    // ScoreManager内で利用する変数
    private readonly Dictionary<int, int> hitCounts = new Dictionary<int, int>();

    public event Action<int> OnScoreChanged;
    public event Action<int> OnHighScoreChanged;

    private const string HighScoreKey = "HighScore";

    /// <summary>
    /// 自身をシングルトン化
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }
    
    // Target側が実行する
    // Targetに球が当たった時に実行し、自身のスコアを引数として代入する
    // そのターゲットの点数だけスコアが上がり、その点数のターゲットのヒット回数を記録する
    public void RegisterTargetHit(int scoreAmount)
    {
        if (hitCounts.ContainsKey(scoreAmount)) hitCounts[scoreAmount]++;
        else hitCounts[scoreAmount] = 1;
        AddScore(scoreAmount);
    }

    // 点数ごとにヒットした回数を取得する
    public int GetHitCount(int scoreAmount)
    {
        return hitCounts.TryGetValue(scoreAmount, out int c) ? c : 0;
    }

    // 現在の点数に足す
    public void AddScore(int amount)
    {
        CurrentScore += amount;
        OnScoreChanged?.Invoke(CurrentScore);

        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();

            OnHighScoreChanged?.Invoke(HighScore);

            Debug.Log($"high score! {HighScore}");
        }
    }

    // スコアのリセットを行う
    public void ResetScore()
    {
        CurrentScore = 0;
        hitCounts.Clear();
        OnScoreChanged?.Invoke(CurrentScore);
    }
}
