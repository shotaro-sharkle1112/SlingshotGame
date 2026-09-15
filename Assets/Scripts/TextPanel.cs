using TMPro;
using UnityEngine;

public class TextPanel : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI text;

    public void SetText(string s)
    {
        if (text != null) text.text = s;
    }
}
