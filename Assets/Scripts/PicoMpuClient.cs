// PicoMpuClient.cs (temperature なし版)
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable] public class Vec3 { public float x, y, z; }
[System.Serializable] public class MpuPayload { public Vec3 accel; public Vec3 gyro; }

public class PicoMpuClient : MonoBehaviour
{
    [Header("Pico W HTTP")]
    public string PicoIp = "192.168.1.23";
    public float PollInterval = 0.05f; // 20Hz

    [Header("Outputs (read-only)")]
    public Vector3 Accel; // g
    public Vector3 Gyro;  // °/s
    public bool LastRequestOk;

    string Url => $"http://{PicoIp}/api/mpu";
    Coroutine loop;

    void OnEnable() { if (loop == null) loop = StartCoroutine(PollLoop()); }
    void OnDisable() { if (loop != null) { StopCoroutine(loop); loop = null; } }

    IEnumerator PollLoop()
    {
        var wait = new WaitForSeconds(PollInterval > 0 ? PollInterval : 0.05f);
        while (true)
        {
            using (var req = UnityWebRequest.Get(Url))
            {
                req.timeout = 2;
                yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool ok = req.result == UnityWebRequest.Result.Success;
#else
                bool ok = !req.isNetworkError && !req.isHttpError;
#endif
                if (ok)
                {
                    try
                    {
                        var data = JsonUtility.FromJson<MpuPayload>(req.downloadHandler.text);
                        if (data?.accel != null && data.gyro != null)
                        {
                            Accel = new Vector3(data.accel.x, data.accel.y, data.accel.z);
                            Gyro  = new Vector3(data.gyro.x,  data.gyro.y,  data.gyro.z);
                            LastRequestOk = true;
                        }
                        else
                        {
                            LastRequestOk = false;
                            Debug.LogWarning("JSON lacks accel/gyro.");
                        }
                    }
                    catch (System.Exception e)
                    {
                        LastRequestOk = false;
                        Debug.LogWarning("Parse error: " + e.Message);
                    }
                }
                else
                {
                    LastRequestOk = false;
                    Debug.LogWarning($"HTTP error: {req.responseCode} {req.error}");
                }
            }
            yield return wait;
        }
    }
}
