// ImuOrientation.cs
// 取得した accel[g], gyro[deg/s] から姿勢を推定して target を回転
using System.Collections;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewSlingshotRotation : MonoBehaviour
{
    [Header("Source (NewControlerManager)")]
    public NewControlerManager Source;      // Accel, Gyro を読む
    public Transform Target;               // 回す対象（未指定なら自分）

    [Header("Keys")]
    public KeyCode keyResetInitialPosture = KeyCode.R; // 初期姿勢をリセットするキー


    public float smoothness = 2.0f;

    // 目標姿勢
    private Quaternion desireQuat;

    // 最初にセンサの初期姿勢を取得したかどうか
    private bool initialized;
    // 最初のセンサの初期姿勢
    private Quaternion initialQuat;

    void Awake()
    {
        if (Target == null) Target = transform;
        desireQuat = Quaternion.identity;
        initialQuat = Quaternion.identity;
    }

    void OnEnable()
    {
        
    }

    void Update()
    {
        if (Source == null) return;
        
        // 初期姿勢で回転を修正する
        desireQuat = Quaternion.Inverse(initialQuat) * Source.Quat;
        // 取得したセンサ値に向かって回転
        Target.rotation = Quaternion.RotateTowards(Target.rotation, desireQuat, smoothness * Time.deltaTime);
        
        if (Input.GetKeyUp(keyResetInitialPosture))
        {
            // 初期姿勢をリセット
            InitializeRotation();
        }
    }

    public void InitializeRotation()
    {
        // 初期姿勢を記録
        initialQuat = Source.Quat;
        Debug.Log("initialized");
    }
}
