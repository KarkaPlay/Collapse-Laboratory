using UnityEngine;

public class ColorSwitcher : MonoBehaviour
{
    public Color baseColor;
    public Color onColor;
    public Color offColor;

    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void SwitchColor(bool isOn)
    {
        _renderer.material.color = isOn ? onColor : offColor;
    }
}
