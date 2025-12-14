using System;
using UnityEngine;

namespace Objects
{
    public class AnimatedCollapsible : Interactable, IAnimatedCollapsible
    {
        private static readonly int MotionTime = Animator.StringToHash("MotionTime");
        
        public Animator animator;
        
        [SerializeField] private float animationSpeed = 1;
        [SerializeField] private float _startAnimationProgress;
        
        private float _animationProgress;

        private void Start()
        {
            SetAnimationProgress(_startAnimationProgress);
        }

        public void Animate(float directionMultiplier)
        {
            float delta = Time.deltaTime * animationSpeed * directionMultiplier;
            SetAnimationProgress(_animationProgress + delta);
        }
        
        private void SetAnimationProgress(float progress)
        {
            if (progress >= 1)
            {
                progress = 0.999f;
            }
            else if (progress < 0)
            {
                progress = 0;
            }
            
            _animationProgress = progress;
            animator.SetFloat(MotionTime, _animationProgress);
            PlayerUI.Instance.UpdateAnimatedCollapsibleSlider(_animationProgress);
        }

        public void SetChildren()
        {
            foreach (Transform child in transform)
            {
                if (!child.gameObject.GetComponent<Collider>())
                {
                    Debug.LogWarning($"У объекта {child.gameObject.name} отсутствует коллайдер, поэтому добавляем ему Box Collider. Если он не подходит, замените на другой");
                }

                if (!child.gameObject.GetComponent<AnimatedCollapsibleChild>())
                {
                    Debug.LogWarning($"Объекту {child.gameObject.name} добавлен AnimatedCollapsibleChild");
                    child.gameObject.AddComponent<AnimatedCollapsibleChild>();
                    child.gameObject.GetComponent<AnimatedCollapsibleChild>().parentAnimatedCollapsible = this;
                }
            }
        }
        
        public float GetAnimationProgress() => _animationProgress;
    }
}