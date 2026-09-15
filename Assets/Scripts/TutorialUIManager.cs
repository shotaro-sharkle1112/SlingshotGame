using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialUIManager : MonoBehaviour
{
    [Header("チュートリアルテキストを表示するパネル")]
    [SerializeField] private GameObject panel;
    // テキストを変更するためのクラス
    private TextPanel textPanel;

    [Header("Tutorial Manager")]
    [SerializeField] private TutorialManager tutorialManager;

    void Start()
    {
        // テキスト編集APIがあるコンポーネントを取得
        textPanel = panel.GetComponent<TextPanel>();
    }

    void Update()
    {
        // チュートリアルの進行状況をTutorialManagerから取得してUIの処理を走らせる
        switch (tutorialManager.tutorialState)
        {
            // チュートリアル開始前
            case TutorialManager.TutorialState.BeforeTutorial:
                textPanel.SetText("チュートリアルへようこそ！\nまずはパチンコの操作を教えますね");
                break;
            case TutorialManager.TutorialState.LookForward:
                textPanel.SetText("まずはコントローラーを握り、\nじっと動かさずに真正面に向けましょう");
                break;
            case TutorialManager.TutorialState.Calibration:
                textPanel.SetText("キャリブレーション中です\nじっと動かさずに真正面に向けましょう");
                break;
            case TutorialManager.TutorialState.RotateToLeft:
                textPanel.SetText("コントローラーをゆっくり左に回転させて、\nパチンコで左を狙ってみましょう");
                break;
            case TutorialManager.TutorialState.RotateToRight:
                textPanel.SetText("コントローラーをゆっくり右に回転させて、\nパチンコで右を狙ってみましょう");
                break;
            case TutorialManager.TutorialState.RotateToUp:
                textPanel.SetText("コントローラーをゆっくり上に向けて、\nパチンコで上を狙ってみましょう");
                break;
            case TutorialManager.TutorialState.RotateToDown:
                textPanel.SetText("コントローラーをゆっくり下に向けて、\nパチンコで下を狙ってみましょう");
                break;
            case TutorialManager.TutorialState.Shot:
                textPanel.SetText("青色のゴムを引っ張って曲げて、離してみましょう\n弾を打てます");
                break;
            case TutorialManager.TutorialState.Charge:
                textPanel.SetText("今度はもっと引っ張って曲げて、\nチャージ音が鳴ってから離してみましょう\n非常に強力なショットになります");
                break;
            case TutorialManager.TutorialState.ResetDirection:
                textPanel.SetText("パチンコとコントローラーの左右の向きがズレてきたら、\nコントローラーを45度下に向けることでリセットできます");
                break;
            case TutorialManager.TutorialState.Targets:
                textPanel.SetText("4種類的があります\n青が100点、オレンジが300点、赤が500点、金が1000点です\n全て打ち倒してみましょう");
                break;
            case TutorialManager.TutorialState.AfterTutorial:
                textPanel.SetText("では本番にいきましょう！");
                break;
            default:
                break;
        }
    }
}

