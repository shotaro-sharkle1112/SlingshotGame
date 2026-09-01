using UnityEngine;
using System;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance {get; private set;}

    public int CurrentScore { get; private set;}
    public int HighScore { get; private set;}

    public IReadOnlyDictionary<int, int> HitCounts => hitCounts;
    private readonly Dictionary<int, int> hitCounts = new Dictionary<int, int>();

    public event Action<int> OnScoreChanged;
    public event Action<int> OnHighScoreChanged;

    private const string HighScoreKey = "HighScore";

    /// <summary>
    /// Awake is called when the script instance is being loaded.
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
    
    public void RegisterTargetHit(int scoreAmount)
    {
        if (hitCounts.ContainsKey(scoreAmount)) hitCounts[scoreAmount]++;
        else hitCounts[scoreAmount] = 1;
        AddScore(scoreAmount);
    }

    public int GetHitCount(int scoreAmount)
    {
        return hitCounts.TryGetValue(scoreAmount, out int c) ? c : 0;
    }

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

    public void ResetScore()
    {
        CurrentScore = 0;
        hitCounts.Clear();
        OnScoreChanged?.Invoke(CurrentScore);
    }
}
