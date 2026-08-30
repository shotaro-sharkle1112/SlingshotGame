using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class PicoBendClient : MonoBehaviour
{
    [SerializeField] private string serverIP = "192.168.11.2";
    [SerializeField] private float pollInterval = 0.1f;
    private int latestBendValue = 0;
    private bool hasBendValue = false;
    private string BendUrl => $"http://{serverIP}/bend";

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(PollBendValue());
    }

    private IEnumerator PollBendValue()
    {
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(BendUrl))
            {
                yield return request.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                bool success = request.result == UnityWebRequest.Result.Success;
#else
                bool success = !request.isNetworkError && !request.isHttpError;
#endif
                if (success)
                {
                    string text = request.downloadHandler.text.Trim();

                    if (int.TryParse(text, out int bendValue))
                    {
                        latestBendValue = bendValue;
                        hasBendValue = true;

                        // Debug.Log($"bend receive: {latestBendValue}");
                    }
                    else
                    {
                        Debug.Log($"Bend value parse failed: {text}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Bend request failed: {request.error}");
                }
            }

            yield return new WaitForSeconds(pollInterval);
        }
    }
    
    public int GetBendValue()
    {
        return latestBendValue;
    }
}
