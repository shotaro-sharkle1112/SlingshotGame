using UnityEngine;
using UnityEngine.UI;

public class ChargeGage : MonoBehaviour
{

    [SerializeField] private Slingshot slingshot;
    [SerializeField] private Slider slider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(slider != null) slider.value = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        slider.value = slingshot.chargeRatio;
    }
}
