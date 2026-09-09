using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Slingshot : MonoBehaviour {

    // 弾の発射速度
    [SerializeField] public float metalSphereVelocity;

    // 生成するmetalsphere用のrigidbodyとmetalsphere(CSスクリプト)
    private Rigidbody metalSphereRigidbody;
    // metalSphereにはオブジェクトの方とスクリプトのクラスを指す方の二者がいるので注意
    private MetalSphere metalSphereScript;

    public GameObject metalSphereObject;

    // パチンコの稼働部位
    public GameObject rightElastic;
    public GameObject leftElastic;
    public GameObject rightLine;
    public GameObject leftLine;
    public GameObject leather;
    public GameObject leatherLine;
    private LineRenderer rightElasticLine;
    private LineRenderer leftElasticLine;

    // 引っ張った時の最長の長さ(ローカルのz座標)、引っ張らない状態は-2になる
    private float pulled;

    // metalsphereが生成されたかどうか
    // 生成されたら0、生成されていなかったら1
    private bool isMetalSphereGenerated;
    private float z;

    //曲げセンサの値を保存する
    //普段は大体22000から23000の間にある
    //もしパチンコを引っ張った場合はマイナスの方向に行くのでsensorMinValueの方向にいく
    // 小(15000くらい) < sensorValue < 大(22000~23000) ===>> 大(-7) > z > 小(-2)　のマッピングになる
    private int sensorValue = 20000;
    // センサの値が小さくなるほどパチンコが伸びるように設定しているので、パチンコの伸びてから戻るまでの最小値を記録し続ける値。伸びが戻ったら大きな値に戻す
    private int sensorStretchMinValue = 25000;
    [SerializeField] private int sensorMaxValue = 23000;
    [SerializeField] private int sensorMinValue = 17000;
    [SerializeField] private int sensorMinThres = 20300;
    [SerializeField] private int sensorMaxThres = 23000;

    //縮んだ状態でボールを発射する仕組みにすると、通常の状態でボールを無限に発射され続けてしまうので、曲げセンサの状態変化を記録するboolを用意する
    private bool isStretched = false;

    // 発射後のクールタイムの設定
    [SerializeField] private float fireCooldown = 0.5f;
    // 発射後何秒経ったかを記録
    private float cooldownRemaining = 0f;

    // コントローラのセンサ値を受け取るためのマネージャー
    [SerializeField] private ControlerManager controlerManager;

    void Start ()
    {      
        isMetalSphereGenerated = false;
        //Starts with z = -2 so that the elastic line starts at the size of the elastic.
        z = -2;

        //If the ElasticManager script is not inside the slingshot, the value of pulled is set to - 7.
        if (this.GetComponent<ElasticManager>() != null)
        {
            //Getting elastic resistance value in the ElasticManager script.
            pulled = this.GetComponent<ElasticManager>().elasticResistance - 8;
        }
        else
        {
            pulled = -7;
        }
    }

    void Update()
    {
        //クールタイム中は何も処理しない（発射後 fireCooldown 秒間）
        if (cooldownRemaining > 0f)
        {
            cooldownRemaining -= Time.deltaTime;
            return;
        }

        //曲げセンサの値を取得
        sensorValue = controlerManager.Bend;

        //曲げセンサの値が一定の範囲外に出たらパチンコの伸び開始
        if (sensorValue <= sensorMinThres)
        {
            //伸び始めたことをbool値で記録
            //これによって弾発射部分のコードのif文内が実行される
            isStretched = true;

            // 伸び始めたら、一番伸びている状態(sensorValueが最小)の状態を記録、更新しておく
            if (sensorStretchMinValue > sensorValue) sensorStretchMinValue = sensorValue;

            // センサ値の曲げしろを0~1にマッピング
            float norm = Mathf.InverseLerp(sensorMinThres, sensorMinValue, sensorStretchMinValue);
            
            // 0~1にマッピングされたセンサ値をローカルのz座標にマッピングする
            z = Mathf.Lerp(-2f, pulled, norm);

            if (!isMetalSphereGenerated)
            {
                //if the isMetalSphereGenerated(increment) is equal to one, it means that there is no metal sphere in the slingshot, then the sphere is created to be thrown next.
                metalSphereObject = Instantiate(metalSphereObject, new Vector3(metalSphereObject.transform.position.x, metalSphereObject.transform.position.y, -3), Quaternion.identity);
                metalSphereRigidbody = metalSphereObject.GetComponent<Rigidbody>();
                metalSphereScript = metalSphereObject.GetComponent<MetalSphere>();
                //The metal sphere is parented to the slingshot, so that it can move with the slingshot.
                metalSphereObject.transform.parent = this.transform;
                isMetalSphereGenerated = true;                           
            }

            //Disables the elastic mesh renderer.
            rightElastic.GetComponent<SkinnedMeshRenderer>().enabled = false;
            leftElastic.GetComponent<SkinnedMeshRenderer>().enabled = false;
            leather.GetComponent<SkinnedMeshRenderer>().enabled = false;

            //Activates the elastic line, so the movement becomes more fluid and beautiful.
            rightLine.SetActive(true);
            leftLine.SetActive(true);
            leatherLine.GetComponent<SkinnedMeshRenderer>().enabled = true;

            rightElasticLine = rightLine.transform.GetComponent<LineRenderer>();
            leftElasticLine = leftLine.transform.GetComponent<LineRenderer>();

            // ゴムの伸びやmetalsphereの生成のz座標を表すzが最大伸びしろpulledよりも大きくなった場合はzに従う
            if (z >= pulled)
            {
                //For the elastic to stretch the value of the z axis is increased to - 7 or pulled value, maximum of the stretch.
                rightElasticLine.SetPosition(1, new Vector3(0, 0, z));
                //The lines are growing and the value of the z axis is increased.
                leftElasticLine.SetPosition(1, new Vector3(0, 0, z));
                //Leather and metallic sphere follow the movement of the line.
                metalSphereObject.transform.localPosition = new Vector3(-1.42f, 2.286f, z + 1.7f);
                leather.transform.localPosition = new Vector3(-1.42f, 2.286f, z + 1.2f);
                leatherLine.transform.localPosition = new Vector3(-1.42f, 2.286f, z + 1.2f);
            }
            else
            {
                //If the z axis value reaches the maximum -7 or pulled value, that value will remain and the slingshot elastic will be completely stretched.
                rightElasticLine.SetPosition(1, new Vector3(0, 0, pulled));
                leftElasticLine.SetPosition(1, new Vector3(0, 0, pulled));
                metalSphereObject.transform.localPosition = new Vector3(-1.42f, 2.286f, pulled + 1.7f); 
                leather.transform.localPosition = new Vector3(-1.42f, 2.286f, pulled + 1.2f); 
                leatherLine.transform.localPosition = new Vector3(-1.42f, 2.286f, pulled + 1.2f); 
            }

        }

        //もしパチンコが伸ばされたらこのif文内が実行される。
        //一度曲げ状態が戻ったらこのif文内は実行されなくなる。
        if (isStretched && sensorValue >= sensorMinThres)
        {
            // "sensorValue >= sensorMinThres"となる状態は曲げていない状態

            // このブロックでは弾の発射処理を行う

            // 弾を発射するので、弾が再生成できる状態にする
            isMetalSphereGenerated = false;

            //Activates the elastic mesh renderer.
            rightElastic.GetComponent<SkinnedMeshRenderer>().enabled = true;
            leftElastic.GetComponent<SkinnedMeshRenderer>().enabled = true;
            leather.GetComponent<SkinnedMeshRenderer>().enabled = true;

            //Disables the elastic line.
            rightLine.SetActive(false);
            leftLine.SetActive(false);
            leatherLine.GetComponent<SkinnedMeshRenderer>().enabled = false;

            //Activates the sphere's gravity as soon as it is thrown.
            metalSphereRigidbody.useGravity = true;

            
            // センサ値の曲げしろを0~1にマッピング
            float norm = Mathf.InverseLerp(sensorMinThres, sensorMinValue, sensorStretchMinValue);

            // 0~1にマッピングされたセンサ値をローカルのz座標にマッピングする
            float stretch = Mathf.Lerp(-2f, pulled, norm);

            metalSphereRigidbody.linearVelocity = Vector3.zero;
            metalSphereRigidbody.angularVelocity = Vector3.zero;

            // 弾の打ち出す力を向きを計算
            Vector3 metalSphereShotForce = transform.forward * metalSphereVelocity;

            if (stretch >= pulled)
            {
                //For the elastic to stretch the value of the z axis is increased to - 7, maximum of the stretch.
                //The force that the metallic sphere will be thrown, will be multiplied by the amount of stretch of the elastic.
                metalSphereShotForce *= -stretch;
            }else
            {
                metalSphereShotForce *= 15;
            }

            // metalsphereを発射
            metalSphereRigidbody.AddForce(metalSphereShotForce, ForceMode.Impulse);

            // エフェクトを再生
            metalSphereScript.EnableParticleEffect();

            //The metal sphere is taken (parent = null) in the slingshot, so that the sphere stops moving with the slingshot and the camera.
            metalSphereObject.transform.parent = null;

            // 各種値をリセット
            z = -2;
            sensorStretchMinValue = 25000;
            isStretched = false;

            // クールダウンタイムを設ける
            cooldownRemaining = fireCooldown;
        }
    }
}
