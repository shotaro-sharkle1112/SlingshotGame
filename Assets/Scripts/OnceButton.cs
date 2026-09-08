using UnityEngine;
using UnityEngine.UI;

// ボタンを押すのが一回きりで終了のものにアタッチするスクリプト
// DisableButtonを
[RequireComponent(typeof(Button))]
public class OnceButton : MonoBehaviour
{
    private Button button;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button = GetComponent<Button>();
        // ボタンを押した時の呼び出しハンドラの登録
        button.onClick.AddListener(DisableButton);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // ボタンを一回押すと反応しなくなる
    public void DisableButton()
    {
        button.interactable = false;
    }
}
