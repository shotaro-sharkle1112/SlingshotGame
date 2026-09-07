using UnityEngine;
using UnityEngine.UI;

public class StartSceneUIManager : MonoBehaviour
{
    [Header("Game Manager")]
    // ゲームの進行状況を管理するゲームマネージャー
    [SerializeField] private StartSceneGameManager gameManager;

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
        if (chargeSlider != null) chargeSlider.value = 0f;

        // 初期状態以外のテキストを一回非表示に
        bendToStartText.SetActive(true);
        keepBendingText.SetActive(false);
        startText.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // スタートしたら画面の描画は止める
        if (gameManager.IsStart) return;
        //ゲームマネージャーからチャージ割合を取得
        var chargeRatio = gameManager.ChargingRatio;

        // スライダーに反映
        if (chargeSlider != null) chargeSlider.value = chargeRatio;
        
        // 文字の描画順
        // bendtostart -> keepbending(charging) -> start
        if (chargeRatio <= 0.01f)
        {
            // チャージ前
            bendToStartText.SetActive(true);
            keepBendingText.SetActive(false);
            startText.SetActive(false);
        }
        else if (chargeRatio < 0.99f)
        {
            // チャージ中
            bendToStartText.SetActive(false);
            keepBendingText.SetActive(true);
            startText.SetActive(false);
        }
        else
        {
            // チャージ後
            bendToStartText.SetActive(false);
            keepBendingText.SetActive(false);
            startText.SetActive(true);
        }
    }
}
