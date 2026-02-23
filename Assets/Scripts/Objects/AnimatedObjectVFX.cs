using System.Collections.Generic;
using UnityEngine;

public class AnimatedObjectVFX : MonoBehaviour
{
    [System.Serializable]
    public class ParticleEffectConfig
    {
        public ParticleSystem particleSystem;
        public AnimationCurve emissionOverProgress = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Range(0, 100)] public float maxEmissionRate = 10f;
    }

    [System.Serializable]
    public class MaterialEffectConfig
    {
        public Renderer targetRenderer;
        public string propertyName = "_EmissionIntensity";
        public AnimationCurve intensityOverProgress = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    [Header("Частицы")]
    [SerializeField] private List<ParticleEffectConfig> particleEffects = new();

    [Header("Материалы")]
    [SerializeField] private List<MaterialEffectConfig> materialEffects = new();

    [Header("Объекты разрушения")]
    [SerializeField] private List<GameObject> debrisToSpawn = new();
    [SerializeField] private AnimationCurve debrisSpawnProgress = AnimationCurve.Linear(0, 0, 1, 1);

    private float _lastProgress = -1f;

    public void UpdateVFX(float animationProgress)
    {
        if (Mathf.Approximately(_lastProgress, animationProgress))
            return;

        _lastProgress = animationProgress;

        UpdateParticles(animationProgress);
        UpdateMaterials(animationProgress);
        UpdateDebris(animationProgress);
    }

    private void UpdateParticles(float progress)
    {
        foreach (var config in particleEffects)
        {
            if (config.particleSystem == null) continue;

            var emission = config.particleSystem.emission;
            float intensity = config.emissionOverProgress.Evaluate(progress);
            emission.rateOverTime = config.maxEmissionRate * intensity;
        }
    }

    private void UpdateMaterials(float progress)
    {
        foreach (var config in materialEffects)
        {
            if (config.targetRenderer == null) continue;

            float intensity = config.intensityOverProgress.Evaluate(progress);

            foreach (var material in config.targetRenderer.materials)
            {
                if (material.HasProperty(config.propertyName))
                {
                    material.SetFloat(config.propertyName, intensity);
                }
            }
        }
    }

    private void UpdateDebris(float progress)
    {
        foreach (var debris in debrisToSpawn)
        {
            if (debris == null) continue;

            float spawnProgress = debrisSpawnProgress.Evaluate(progress);
            debris.SetActive(spawnProgress > 0.01f);
        }
    }

    public void ResetVFX()
    {
        _lastProgress = -1f;
        UpdateVFX(0f);
    }
}
