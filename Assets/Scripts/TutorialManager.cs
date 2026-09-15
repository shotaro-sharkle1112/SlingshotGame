using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class TutorialManager : MonoBehaviour
{
    [Header("パチンコ")]
    [SerializeField] private GameObject slingshot;
    [SerializeField] private Slingshot slingshotScript;
    [SerializeField] private SlingshotRotation slingshotRotation;

    [Header("マト")]
    [SerializeField] private GameObject TargetBluePrefab;
    [SerializeField] private GameObject TargetOrangePrefab;
    [SerializeField] private GameObject TargetRedPrefab;
    [SerializeField] private GameObject TargetGoldPrefab;

    [Header("Sound")]
    // チュートリアルが一個進んだら鳴らすジングル
    [SerializeField] private AudioClip tutorialStepAdvanceJingle;
    // チュートリアルが一個クリアできたら鳴らすジングル
    [SerializeField] private AudioClip tutorialStepClearJingle;
    [SerializeField] private AudioSource jingleAudioSource;

    [Header("次の読み込むシーン名")]
    [SerializeField] private string nextSceneName = "StageSelectScene";

    // チュートリアルの進行状況を記録する

    public enum TutorialState
    {
        // チュートリアルが始まる前
        BeforeTutorial,
        // 正面に向ける
        LookForward,
        // キャリブレーション
        Calibration,
        // 左に向ける
        RotateToLeft,
        //右に向ける
        RotateToRight,
        // 上に向ける
        RotateToUp,
        // 下に向ける
        RotateToDown,
        // 曲げて発射
        Shot,
        // チャージ
        Charge,
        // 向きのリセット
        ResetDirection,
        // 全種類の的の点数を当てて確認する
        Targets,
        // チュートリアル終了
        AfterTutorial
    }

    public TutorialState tutorialState {get; private set;}

    // チュートリアルの進行のガードになる
    private bool isWaiting;

    // マトを生成し終わったか
    private bool isTargetGenerated;

    // 生成した全ての的をリストに入れる
    private List<GameObject> targets = new List<GameObject>();

    void Start()
    {
        // チュートリアルが始まる前
        tutorialState = TutorialState.BeforeTutorial;

        // 変数のリセット
        isWaiting = false;
        isTargetGenerated = false;
    }

    private void Update() {
        Vector3 slingshotEuler = slingshot.transform.rotation.eulerAngles;
        switch (tutorialState)
        {
            // チュートリアル開始前
            case TutorialState.BeforeTutorial:
                TryStepNextTutorial();
                break;
            case TutorialState.LookForward:
                if (Vector3.Dot(slingshot.transform.forward, Vector3.forward) > 0.99f) TryStepNextTutorial();
                break;
            case TutorialState.Calibration:
                StartCoroutine(slingshotRotation.CalibGyroBias());
                TryStepNextTutorial();
                break;
            case TutorialState.RotateToLeft:
                if (Mathf.DeltaAngle(0f, slingshotEuler.y) < -45f) TryStepNextTutorial();
                break;
            case TutorialState.RotateToRight:
                if (Mathf.DeltaAngle(0f, slingshotEuler.y) > 45f) TryStepNextTutorial();
                break;
            case TutorialState.RotateToUp:
                if (Mathf.DeltaAngle(0f, slingshotEuler.x) < -30f) TryStepNextTutorial();
                break;
            case TutorialState.RotateToDown:
                if (Mathf.DeltaAngle(0f, slingshotEuler.x) > 30f) TryStepNextTutorial();
                break;
            case TutorialState.Shot:
                if (slingshotScript.slingshotState == Slingshot.SlingshotState.Bending) TryStepNextTutorial();
                break;
            case TutorialState.Charge:
                if (slingshotScript.slingshotState == Slingshot.SlingshotState.Charging) TryStepNextTutorial();
                break;
            case TutorialState.ResetDirection:
                TryStepNextTutorial();
                break;
            case TutorialState.Targets:
                if (!isTargetGenerated)
                {
                    // インスタンスの生成 
                    targets.Add(Instantiate(TargetBluePrefab, new Vector3(-25f, 15f, 60f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetBluePrefab, new Vector3(-25f, -5f, 60f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetOrangePrefab, new Vector3(-10f, 15f, 60f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetOrangePrefab, new Vector3(-10f, -5f, 60f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetRedPrefab, new Vector3(5f, 15f, 60f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetRedPrefab, new Vector3(5f, -5f, 60f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetGoldPrefab, new Vector3(20f, 15f, 60f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetGoldPrefab, new Vector3(20f, -5f, 60f), TargetRedPrefab.transform.rotation));

                    // 全ての的を大きくする
                    foreach (GameObject t in targets)
                    {
                        t.transform.localScale *= 1.5f;
                    }

                    // 生成は一回だけ
                    isTargetGenerated = true;
                }

                // 破壊されたオブジェクトは全てnullになるので除外
                targets.RemoveAll(t => t == null);
                // もし破壊されずにどこかへ行こうとした場合は除外
                targets.RemoveAll(t => t.transform.position.magnitude > 100f);

                if (isTargetGenerated && targets.Count == 0) TryStepNextTutorial();
                break;
            case TutorialState.AfterTutorial:
                StartCoroutine(NextScene(nextSceneName));
                break;
            default:
                break;
        }
    }


    // 5秒間経った後チュートリアルを進行させる
    IEnumerator StepNextTutorial()
    {
        yield return new WaitForSeconds(5);
        tutorialState += 1;
        isWaiting = false;
        PlayJingle(tutorialStepAdvanceJingle);
    }

    // 次のチュートリアルに進む
    void TryStepNextTutorial()
    {
        if (isWaiting) return;
        isWaiting = true;
        PlayJingle(tutorialStepClearJingle);
        StartCoroutine(StepNextTutorial());
    }

    // 次のシーンに進む
    IEnumerator NextScene(string s)
    {
        yield return new WaitForSeconds(5);
        SceneManager.LoadScene(s);
    }

    // ジングルを鳴らす関数
    void PlayJingle(AudioClip audioClip)
    {
        if (jingleAudioSource != null && audioClip != null) jingleAudioSource.PlayOneShot(audioClip);
    }
}
