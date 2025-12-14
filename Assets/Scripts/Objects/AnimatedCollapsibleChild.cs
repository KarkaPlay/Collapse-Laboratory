using UnityEngine;

namespace Objects
{
    public class AnimatedCollapsibleChild : MonoBehaviour, IAnimatedCollapsible
    {
        public AnimatedCollapsible parentAnimatedCollapsible;
        
        public void Animate(float directionMultiplier) => parentAnimatedCollapsible.Animate(directionMultiplier);

        public void OnHighlight() => parentAnimatedCollapsible.OnHighlight();

        public void OnUnhighlight() => parentAnimatedCollapsible.OnUnhighlight();
    }
}