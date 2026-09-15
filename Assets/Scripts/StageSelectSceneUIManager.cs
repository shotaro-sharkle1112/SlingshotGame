using UnityEngine;
using UnityEngine.UI;

public class StageSelectSceneUIManager : MonoBehaviour
{
    [Header("Game Manager")]
    // ゲームの進行状況を管理するゲームマネージャー
    [SerializeField] private StageSelectSceneGameManager gameManager;

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
