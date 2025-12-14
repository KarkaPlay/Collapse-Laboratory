using UnityEngine;
using UnityEngine.UI;
using Utils;

public class PlayerUI : SingletonBehaviour<PlayerUI>
{
    public Slider animatedCollapsibleSlider;
    
    private void Start()
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
