using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Slingshot : MonoBehaviour {

    // 弾の発射速度
    [SerializeField] public float metalSphereVelocity;

    // 生成するmetalsphere用のrigidbodyとmetalsphere(CSスクリプト)
    private Rigidbody metalSphereRigidbody;
    // metalSphereにはオブジェクトの方とスクリプトのクラスを指す方の二者がいるので注意
    private MetalSphere metalSphereScript;

    [Header("パチンコが飛ばす弾 Prefab")]
    public GameObject metalSpherePrefab;
    private GameObject metalSphereObject;

    [Header("パチンコのゴムの部分")]
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
    
    [Header("曲げセンサの設定項目")]
    // センサーの取りうる最小の値
    [SerializeField] private int sensorMinValue = 17000;
    // チャージを開始するか判定を行う曲げセンサの値、この値より小さいとパチンコのチャージ状態に移行する
    [SerializeField] private int sensorChargeThres = 13000;
    // パチンコが伸びるべきか判定を行う曲げセンサの値, この値より小さいとパチンコの伸びが開始する
    [SerializeField] private int sensorStretchThres = 20300;

    //縮んだ状態でボールを発射する仕組みにすると、通常の状態でボールを無限に発射され続けてしまうので、曲げセンサの状態変化を記録するboolを用意する
    private bool isStretched = false;

    [Header("発射後のクールタイム")]
    // 発射後のクールタイムの設定
    [SerializeField] private float fireCooldown = 0.5f;
    // 発射後何秒経ったかを記録
    private float cooldownRemaining = 0f;

    [Header("チャージショットの設定項目")]
    // 何秒チャージしたらチャージショットが打てるか
    [SerializeField] private float chargeTimeThres = 3f;
    // チャージをした時間
    private float chargeTime;

    // チャージ完了したかをチェック
    private bool isChargeCompleted => chargeTime > chargeTimeThres;
    // チャージ完了サウンドを行なったかをチェック
    private bool isChargeCompletedSoundPlayed;

    [Header("Sounds")]
    
    // チャージ中のサウンド
    [SerializeField] private AudioClip chargingSound;
    // チャージ完了をユーザーに示すためのサウンド
    [SerializeField] private AudioClip chargeCompletedSound;
    // チャージ完了後の発射のサウンド
    [SerializeField] private AudioClip chargeShotSound;
    // 通常時の引き絞るサウンド
    [SerializeField] private AudioClip elasticStretchSound;
    // チャージしていない時の発射のサウンド
    [SerializeField] private AudioClip normalShotSound;
    // ジングル用のAudioSource
    [SerializeField] private AudioSource jingleAudioSource;
    // ゴムが伸びている音のAudioSource
    [SerializeField] private AudioSource elasticStretchAudioSource;

    

    [Header("コントローラーマネージャー")]

    // コントローラのセンサ値を受け取るためのマネージャー
    [SerializeField] private ControlerManager controlerManager;

    void Start ()
    {      
        // チャージをした時間の初期化
        chargeTime = 0f;

        isMetalSphereGenerated = false;

        isChargeCompletedSoundPlayed = false;
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
        if (sensorValue <= sensorStretchThres)
        {
            //伸び始めたことをbool値で記録
            //これによって弾発射部分のコードのif文内が実行される
            isStretched = true;

            // パチンコが伸びている間、ゴムの伸びているサウンドを鳴らす
            if (!elasticStretchAudioSource.isPlaying) elasticStretchAudioSource.Play();

            // 伸び始めたら、一番伸びている状態(sensorValueが最小)の状態を記録、更新しておく
            if (sensorStretchMinValue > sensorValue) sensorStretchMinValue = sensorValue;

            // センサ値の曲げしろを0~1にマッピング
            float norm = Mathf.InverseLerp(sensorStretchThres, sensorMinValue, sensorStretchMinValue);
            
            // 0~1にマッピングされたセンサ値をローカルのz座標にマッピングする
            z = Mathf.Lerp(-2f, pulled, norm);

            // metalsphereが生成されていなければ生成する
            if (!isMetalSphereGenerated)
            {
                //if the isMetalSphereGenerated(increment) is equal to one, it means that there is no metal sphere in the slingshot, then the sphere is created to be thrown next.
                metalSphereObject = Instantiate(metalSpherePrefab, new Vector3(metalSpherePrefab.transform.position.x, metalSpherePrefab.transform.position.y, -3), Quaternion.identity);
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


            // ゴムの伸びやmetalsphereの生成のz座標を表すzが最大伸びしろpulledよりも大きくなった場合はzに従う(z座標がマイナスなので、不等号の向きは逆)
            float zOffset = z >= pulled ? z : pulled;

            rightElasticLine.SetPosition(1, new Vector3(0, 0, zOffset));
            //The lines are growing and the value of the z axis is increased.
            leftElasticLine.SetPosition(1, new Vector3(0, 0, zOffset));
            //Leather and metallic sphere follow the movement of the line.
            metalSphereObject.transform.localPosition = new Vector3(-1.42f, 2.286f, zOffset + 1.7f);
            leather.transform.localPosition = new Vector3(-1.42f, 2.286f, zOffset + 1.2f);
            leatherLine.transform.localPosition = new Vector3(-1.42f, 2.286f, zOffset + 1.2f);
        }

        // チャージの判定
        if (sensorValue < sensorChargeThres)
        {
            chargeTime = Mathf.Min(chargeTimeThres + 0.1f, chargeTime + Time.deltaTime);
        }

        if (isChargeCompleted && !isChargeCompletedSoundPlayed)
        {
            isChargeCompletedSoundPlayed = true;
            PlayJingle(chargeCompletedSound);
        } 

        // パチンコが伸びた状態から通常状態に戻った時に実行される
        // 一度曲げ状態が戻ったらこのif文内は実行されなくなる。
        if (isStretched && sensorValue >= sensorStretchThres)
        {
            // "sensorValue >= sensorStretchThres"となる状態は曲げていない状態

            // このブロックでは弾の発射処理を行う

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
            float norm = Mathf.InverseLerp(sensorStretchThres, sensorMinValue, sensorStretchMinValue);

            // 0~1にマッピングされたセンサ値をローカルのz座標にマッピングする
            float stretch = Mathf.Lerp(-2f, pulled, norm);

            metalSphereRigidbody.linearVelocity = Vector3.zero;
            metalSphereRigidbody.angularVelocity = Vector3.zero;

            // 弾の打ち出す力を向きを計算
            // チャージでの変更点：威力を強く
            float power = isChargeCompleted ? 1.5f : 1f;
            Vector3 metalSphereShotForce = transform.forward * metalSphereVelocity * power;

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
            // チャージでの変更点：当たり判定を大きく
            float metalSphereScale = isChargeCompleted ? 2f : 1f;
            metalSphereObject.transform.localScale *= metalSphereScale;
            metalSphereRigidbody.AddForce(metalSphereShotForce, ForceMode.Impulse);

            // エフェクトを再生
            // チャージでの変更点：何かしら特別なエフェクトを再生
            metalSphereScript.EnableParticleEffect();

            //The metal sphere is taken (parent = null) in the slingshot, so that the sphere stops moving with the slingshot and the camera.
            metalSphereObject.transform.parent = null;

            // 発射音の再生
            // チャージでの変更点：チャージ完了での発射のサウンドにかえる
            if (isChargeCompleted)
            {
                PlayJingle(chargeShotSound);
            }
            else
            {
                PlayJingle(normalShotSound);
            }

            // ゴムの伸びる音はもういらないので止める
            if (elasticStretchAudioSource.isPlaying) elasticStretchAudioSource.Stop();

            // 各種値をリセット
            z = -2;
            sensorStretchMinValue = 25000;
            isStretched = false;

            // クールダウンタイムを設ける
            cooldownRemaining = fireCooldown;

            // 弾を発射したので、次パチンコが伸びた時に弾が再生成できる状態にする
            isMetalSphereGenerated = false;

            // チャージタイムのリセット
            chargeTime = 0f;

            // チャージ完了通知サウンドのboolをリセット
            isChargeCompletedSoundPlayed = false;
        }
    }

    // ジングルを鳴らすための関数
    private void PlayJingle(AudioClip audioClip)
    {
        if (jingleAudioSource == null || audioClip == null) return;
        jingleAudioSource.PlayOneShot(audioClip);
    }

   void OnValidate()
   {
        // 普通だったら sensorMinValue < sensorChargeThres < sensorStretchThresと設定しなければいけない
        // これをインスペクタ上で守っていない場合は自動で値を変更する
        // sensorMinValueは固定して、他の違反している値を変化させる
        if (sensorMinValue > sensorChargeThres)
        {
            sensorChargeThres = sensorMinValue + 1;
        }

        if (sensorChargeThres > sensorStretchThres)
        {
            sensorStretchThres = sensorChargeThres + 1;
        }
   }
}
