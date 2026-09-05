using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

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
    public Quaternion Quat => new Quaternion(quatX, quatY, quatZ, quatW);
    public int Bend => bend;                                     // 曲げセンサ raw値

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
            serialPort.ReadTimeout = 100;
            serialPort.NewLine = "\n";
            serialPort.Open();

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