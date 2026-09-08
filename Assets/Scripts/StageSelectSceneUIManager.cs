using UnityEngine;
using UnityEngine.UI;

public class StageSelectSceneUIManager : MonoBehaviour
{
    [Header("Game Manager")]
    // ゲームの進行状況を管理するゲームマネージャー
    [SerializeField] private StageSelectSceneGameManager gameManager;

    // UI関連
    [Header("UI Objects")]
    // チャージ中のスライダー
    [SerializeField] private Slider chargeSlider;

    // "Bend to Start"
    [SerializeField] private GameObject bendToStartText;
    // "Keep Bending"
    [SerializeField] private GameObject keepBendingText;
    // スタートの文字
    [SerializeField] private GameObject startText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // スタートしたら画面の描画は止める
        if (gameManager.IsStart) return;

    }
}
