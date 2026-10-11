using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class TutorialUIManager : MonoBehaviour
{
    [Header("チュートリアルテキストを表示するパネル")]
    [SerializeField] private GameObject panel;
    // テキストを変更するためのクラス
    private TextPanel textPanel;

    // videoフレーム
    [SerializeField] private GameObject videoFrame;

    private VideoPlayer videoPlayer;

    [Header("Tutorial Manager")]
    [SerializeField] private TutorialManager tutorialManager;

    void Start()
    {
        // テキスト編集APIがあるコンポーネントを取得
        textPanel = panel.GetComponent<TextPanel>();

        // 下に向けてリセットをする映像を流すためのコンポーネントを取得
        videoPlayer = videoFrame.GetComponent<VideoPlayer>();

        // ビデオを最初非表示に
        videoFrame.SetActive(false);

    }

    void Update()
    {
        // チュートリアルの進行状況をTutorialManagerから取得してUIの処理を走らせる
        switch (tutorialManager.tutorialState)
        {
            // チュートリアル開始前
            case TutorialManager.TutorialState.BeforeTutorial:
                textPanel.SetText("チュートリアルへようこそ！\nお前には的当て名人になってもらうぞ");
                break;
            case TutorialManager.TutorialState.Shot:
                textPanel.SetText("青色のゴムを曲げて放て！\nマトをたおせ");
                break;
            case TutorialManager.TutorialState.RotateToDown:
                videoFrame.SetActive(true);
                videoPlayer.Play();
                textPanel.SetText("向きがズレる？\nコントローラを下に倒し、向きをリセットだ");
                break;
            case TutorialManager.TutorialState.Charge:
                videoFrame.SetActive(false);
                videoPlayer.Stop();
                textPanel.SetText("もっと引っ張ってチャージゲージを貯めろ！\nマトを一掃できるぞ！");
                break;
            case TutorialManager.TutorialState.AfterTutorial:
                textPanel.SetText("「習うより慣れろ」だ\n本番に行くぞ！");
                break;
            default:
                break;
        }
    }
}

