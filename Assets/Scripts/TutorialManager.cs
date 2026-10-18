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
        // 正面、右、左のマトを打つ
        Shot,
        // 下に向ける(リセットの体験も行う)
        RotateToDown,
        // チャージ(一掃できそうなマトの群衆も生成する)
        Charge,
        // チュートリアル終了
        AfterTutorial
    }

    public TutorialState tutorialState {get; private set;}

    // チュートリアルの進行のガードになる
    private bool isWaiting;

    // マトを生成し終わったか
    private bool isTargetGenerated;

    // チャージをしたことがあるか
    private bool isCharged;

    // 生成した全ての的をリストに入れる
    private List<GameObject> targets = new List<GameObject>();

    // チャージのチュートリアルで、全ての的をチャージショットで射抜いたか
    private bool chargeStepClear;


    void Start()
    {
        // チュートリアルが始まる前
        tutorialState = TutorialState.BeforeTutorial;

        // 変数のリセット
        isWaiting = false;
        isTargetGenerated = false;
        isCharged = false;
        chargeStepClear = false;
    }

    private void Update() {
        Vector3 slingshotEuler = slingshot.transform.rotation.eulerAngles;
        switch (tutorialState)
        {
            // チュートリアル開始前
            case TutorialState.BeforeTutorial:
                TryStepNextTutorial();
                break;
            case TutorialState.Shot:
                if (!isTargetGenerated)
                {
                    // インスタンスの生成 
                    targets.Add(Instantiate(TargetBluePrefab, new Vector3(-28f, 0f, 60f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetOrangePrefab, new Vector3(0f, 0f, 60f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetRedPrefab, new Vector3(28f, 0f, 60f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetGoldPrefab, new Vector3(0f, 20f, 60f), TargetRedPrefab.transform.rotation));

                    // 全ての的を大きくする
                    foreach (GameObject t in targets)
                    {
                        t.transform.localScale *= 1.5f;
                    }

                    // 生成は一回だけ
                    isTargetGenerated = true;
                }


                // 破壊されたオブジェクトは除外
                targets.RemoveAll(t => t == null);
                // もし破壊されずにどこかへ行こうとした場合は除外
                targets.RemoveAll(t => t.transform.position.magnitude > 100f);



                if (isTargetGenerated && targets.Count == 0) TryStepNextTutorial();
                break;
            case TutorialState.RotateToDown:
                if (Mathf.DeltaAngle(0f, slingshotEuler.x) > 30f) TryStepNextTutorial();
                break;
            case TutorialState.Charge:
                // TODO(#67, blocked by #56, #64) : Chargeステップの判定をチャージ弾一発4個撃破にする
                if (slingshotScript.slingshotState == Slingshot.SlingshotState.Charging) isCharged = true;
                if (!isTargetGenerated)
                {
                    // インスタンスの生成 
                    targets.Add(Instantiate(TargetBluePrefab, new Vector3(0f, 0f, 60f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetOrangePrefab, new Vector3(0f, 0f, 65f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetRedPrefab, new Vector3(0f, 0f, 75f), TargetRedPrefab.transform.rotation));
                    targets.Add(Instantiate(TargetGoldPrefab, new Vector3(0f, 0f, 80f), TargetRedPrefab.transform.rotation));

                    // 奥の的ほど大きくする
                    targets[0].transform.localScale *= 1.0f;
                    targets[1].transform.localScale *= 1.5f;
                    targets[2].transform.localScale *= 2.0f;
                    targets[3].transform.localScale *= 2.5f;

                    // 生成は一回だけ
                    isTargetGenerated = true;
                }

                // シーン上にある全ての弾を取得する
                var metalSpheres = GameObject.FindGameObjectsWithTag("Sphere");
                foreach (GameObject metalsphere in metalSpheres)
                {
                    var metalSphereScript = metalsphere.GetComponent<MetalSphere>();
                    chargeStepClear |= metalSphereScript.GetHitCount() == 4 && metalSphereScript.isChargeShot;
                }

                if (chargeStepClear)
                {
                    TryStepNextTutorial();
                }
                else
                {
                    // 破壊、もしくはどこかへ飛んでいってしまったオブジェクトを再生成する
                    var idx = targets.FindIndex(t => t == null || t.transform.position.magnitude > 100f);
                    
                    if (idx == 0)
                    {
                        targets[idx] = Instantiate(TargetBluePrefab, new Vector3(0f, 0f, 60f), TargetRedPrefab.transform.rotation);
                        targets[0].transform.localScale *= 1.0f;
                    }
                    else if (idx == 1)
                    {
                        targets[idx] = Instantiate(TargetOrangePrefab, new Vector3(0f, 0f, 65f), TargetRedPrefab.transform.rotation);
                        targets[1].transform.localScale *= 1.5f;
                    }
                    else if (idx == 2)
                    {
                        targets[idx] = Instantiate(TargetRedPrefab, new Vector3(0f, 0f, 75f), TargetRedPrefab.transform.rotation);
                        targets[2].transform.localScale *= 2.0f;
                    }
                    else if (idx == 3)
                    {
                        targets[idx] = Instantiate(TargetGoldPrefab, new Vector3(0f, 0f, 80f), TargetRedPrefab.transform.rotation);
                        targets[3].transform.localScale *= 2.5f;
                    }
                }
                
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
        isTargetGenerated = false;
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
