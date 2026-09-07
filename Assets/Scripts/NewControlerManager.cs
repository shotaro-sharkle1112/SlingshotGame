using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Numerics;
using System.Threading;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ===============================
// Inspector ReadOnly Attribute
// ===============================


public class NewControlerManager : MonoBehaviour
{
    [Header("Serial Settings")]
    public string portName = "/dev/cu.usbmodem1101";
    public int baudRate = 115200;

    [Header("Axis Mapping")]
    [SerializeField] private Vector3 axisSign = new Vector3(1f,1f,1f);
    public enum Axis { X, Y, Z }
    [SerializeField] private Axis upAxis = Axis.X;
    [SerializeField] private Axis fowardAxis = Axis.Y;
    [SerializeField] private Axis rightAxis = Axis.Z;

    [Header("Status")]
    [ReadOnly, SerializeField] private bool isConnected = false;
    [ReadOnly, SerializeField] private int receivedCount = 0;
    [ReadOnly, SerializeField] private string lastJsonLine = "";
    [ReadOnly, SerializeField] private string lastError = "";

    [Header("Bend Sensor")]
    [ReadOnly, SerializeField] private int tMs = 0;
    [ReadOnly, SerializeField] private int bend = 0;

    [Header("Acceleration")]
    [ReadOnly, SerializeField] private float accelX = 0f;
    [ReadOnly, SerializeField] private float accelY = 0f;
    [ReadOnly, SerializeField] private float accelZ = 0f;

    [Header("Gyro")]
    [ReadOnly, SerializeField] private float gyroX = 0f;
    [ReadOnly, SerializeField] private float gyroY = 0f;
    [ReadOnly, SerializeField] private float gyroZ = 0f;

    [Header("Magnetic")]
    [ReadOnly, SerializeField] private float magX = 0f;
    [ReadOnly, SerializeField] private float magY = 0f;
    [ReadOnly, SerializeField] private float magZ = 0f;

    // コントローラ側で姿勢推定した結果のクォータ二オン
    [Header("Quaternion")]
    [ReadOnly, SerializeField] private float quatX = 0f;
    [ReadOnly, SerializeField] private float quatY = 0f;
    [ReadOnly, SerializeField] private float quatZ = 0f;
    [ReadOnly, SerializeField] private float quatW = 0f;

    // SlingshotRotation.cs 等から読み取る公開API
    public Vector3 Accel => new Vector3(accelX, accelY, accelZ);
    public Vector3 Gyro  => new Vector3(gyroX, gyroY, gyroZ);
    public Vector3 Mag => new Vector3(magX, magY, magZ);
    
    // 有効なquatを受信済みか
    private bool hasQuat;
    public bool HasQuat => hasQuat;
    public int Bend => bend;                                     // 曲げセンサ raw値

    public Quaternion Quat
    {
        get
        {
            if (!hasQuat) return Quaternion.identity;

            // ベクトル部分を軸マッピングに従って並べ替えと符号を反転する
            Vector3 v = MapAxes(new Vector3(quatX, quatY, quatZ));

            // 鏡映を含むマッピングなら回転方向を反転
            if (MappingDeterminant() < 0f) v = -v;

            return new Quaternion(v.x, v.y, v.z, quatW);
        }
    }

    private Vector3 MapAxes(Vector3 v)
    {
        return new Vector3(
            GetAxis(v, rightAxis) * axisSign.x,
            GetAxis(v, upAxis) * axisSign.y,
            GetAxis(v, fowardAxis) * axisSign.z
        );
    }

    private static float GetAxis(Vector3 v, Axis a) =>
        a == Axis.X ? v.x : a == Axis.Y ? v.y : v.z;

    private float MappingDeterminant()
    {
        return PermSign(rightAxis, upAxis, fowardAxis) * axisSign.x * axisSign.y * axisSign.z;
    }

    private static float PermSign(Axis a, Axis b, Axis c)
    {
        if (a == b || b == c || c == a) return 0f;
        return (((int)b - (int)a + 3) % 3 == 1) ? 1f : -1f;
    }

    private SerialPort serialPort;
    private Thread readThread;
    private bool isRunning = false;

    private readonly object queueLock = new object();
    private readonly Queue<string> lineQueue = new Queue<string>();

    [Serializable]
    public class SensorPacket
    {
        public int t_ms;
        public int bend;
        public Vector3Data accel;
        public Vector3Data gyro;
        public Vector3Data mag;
        public QuaternionData quat;
        public string error;
    }

    [Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class QuaternionData
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    void Start()
    {
        OpenSerial();
    }


    void FixedUpdate()
    {
        while (true)
        {
            string line = null;

            lock (queueLock)
            {
                if (lineQueue.Count > 0)
                {
                    line = lineQueue.Dequeue();
                }
            }

            if (line == null)
                break;

            ParseLine(line);
        }
    }

    private void OpenSerial()
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 500;
            // コントローラ側の受信を受け取りたいよという意思 dtr = Data Terminal Ready
            serialPort.DtrEnable = true;

            // コントローラに贈りたいよという意思 rts = Request To Send
            serialPort.RtsEnable = true;
            serialPort.NewLine = "\n";
            serialPort.Open();

            // ポートを開けると最初に無駄なデータ(ヘッダ)が送信されるので捨てる
            // ほんとはヘッダを別ポートに吐き捨てる方法があるらしいけど、一旦めんどくさいので捨てるだけにする
            serialPort.ReadLine();

            isRunning = true;
            isConnected = true;
            lastError = "";

            readThread = new Thread(ReadSerialLoop);
            readThread.IsBackground = true;
            readThread.Start();

            Debug.Log("Serial opened: " + portName);
        }
        catch (Exception e)
        {
            isConnected = false;
            lastError = e.Message;
            Debug.LogError("Failed to open serial port: " + e.Message);
        }
    }

    private void ReadSerialLoop()
    {
        while (isRunning)
        {
            try
            {
                string line = serialPort.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                lock (queueLock)
                {
                    lineQueue.Enqueue(line);
                }
            }
            catch (TimeoutException)
            {
                // 無視
            }
            catch (Exception e)
            {
                lock (queueLock)
                {
                    lineQueue.Enqueue("{\"error\":\"" + EscapeJson(e.Message) + "\"}");
                }
            }
        }
    }

    private void ParseLine(string line)
    {
        lastJsonLine = line;

        try
        {
            SensorPacket packet = JsonUtility.FromJson<SensorPacket>(line);
            receivedCount++;

            if (!string.IsNullOrEmpty(packet.error))
            {
                lastError = packet.error;
                return;
            }

            lastError = "";

            tMs = packet.t_ms;
            bend = packet.bend;

            if (packet.accel != null)
            {
                accelX = packet.accel.x;
                accelY = packet.accel.y;
                accelZ = packet.accel.z;
            }

            if (packet.gyro != null)
            {
                gyroX = packet.gyro.x;
                gyroY = packet.gyro.y;
                gyroZ = packet.gyro.z;
            }

            if (packet.mag != null)
            {
                magX = packet.mag.x;
                magY = packet.mag.y;
                magZ = packet.mag.z;
            }

            if (packet.quat != null)
            {
                quatX = packet.quat.x;
                quatY = packet.quat.y;
                quatZ = packet.quat.z;
                quatW = packet.quat.w;
                // 有効なクォータニオンの値が入ったのでtrueに切り替える
                hasQuat = true;
            }

        }
        catch (Exception e)
        {
            lastError = "JSON parse error: " + e.Message;
        }
    }

    private static string EscapeJson(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    void OnDestroy()
    {
        CloseSerial();
    }

    void OnApplicationQuit()
    {
        CloseSerial();
    }

    private void CloseSerial()
    {
        isRunning = false;
        isConnected = false;

        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join(500);
            readThread = null;
        }

        if (serialPort != null)
        {
            try
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
            }
            catch { }

            serialPort = null;
        }
    }
}