using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    public Slider animatedCollapsibleSlider;
    
    public static PlayerUI Instance;
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }
    
    void Start()
    {
        CheckAllElements();
    }

    private void CheckAllElements()
    {
        if (animatedCollapsibleSlider == null)
            Debug.LogError("Slider не назначен");
    }
    
    public void UpdateAnimatedCollapsibleSlider(float value)
    {
        animatedCollapsibleSlider.value = value;
    }
    
    public void SetAnimatedTargetSliderVisible(bool isVisible)
    {
        animatedCollapsibleSlider.gameObject.SetActive(isVisible);
    }
}
