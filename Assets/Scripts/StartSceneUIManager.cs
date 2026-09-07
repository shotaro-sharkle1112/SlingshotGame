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
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (chargeSlider != null) chargeSlider.value = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (chargeSlider != null) chargeSlider.value = gameManager.ChargingRatio;
    }
}
