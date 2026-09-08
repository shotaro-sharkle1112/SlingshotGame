using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OnceButton : MonoBehaviour
{
    private Button button;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button = GetComponent<Button>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DisableButton()
    {
        button.interactable = false;
    }
}
