using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ControlerDebug : MonoBehaviour
{
    [SerializeField] private GameObject slingshot;
    private TextMeshProUGUI debugtmpro;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        debugtmpro = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        debugtmpro.text = $"{slingshot.transform.rotation.eulerAngles}";
    }
}
