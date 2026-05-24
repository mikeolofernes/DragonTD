using UnityEngine;

namespace DragonTD.Core
{
    [CreateAssetMenu(fileName = "GameDirectorConfig", menuName = "DragonTD/GameDirectorConfig")]
    public class GameDirectorConfig : ScriptableObject
    {
        [Header("Intensity Curve")]
        [Tooltip("How quickly intensity rises when enemies are active (per second).")]
        [Range(0f, 1f)] public float IntensityRiseRate = 0.08f;
        [Tooltip("How quickly intensity decays when no enemies are alive (per second).")]
        [Range(0f, 1f)] public float IntensityDecayRate = 0.12f;
        [Tooltip("Intensity spike added each time the player loses a life.")]
        [Range(0f, 0.5f)] public float LifeLostIntensitySpike = 0.15f;
        [Tooltip("Intensity drop when a wave is cleared.")]
        [Range(0f, 0.5f)] public float WaveClearedIntensityDrop = 0.25f;

        [Header("Difficulty Scaling")]
        [Tooltip("Minimum enemy stat multiplier (applied to HP, attack, and speed).")]
        [Range(0.5f, 1f)] public float MinDifficultyMultiplier = 0.75f;
        [Tooltip("Maximum enemy stat multiplier.")]
        [Range(1f, 3f)] public float MaxDifficultyMultiplier = 2.0f;
        [Tooltip("How many consecutive waves cleared perfectly (no lives lost) before difficulty rises.")]
        public int PerfectWavesPerDifficultyStep = 2;
        [Tooltip("How many lives lost in a wave before difficulty drops to give the player relief.")]
        public int LivesLostForRelief = 3;

        [Header("Elite Enemy Events")]
        [Tooltip("Intensity threshold above which elite enemies can spawn (0–1).")]
        [Range(0f, 1f)] public float EliteSpawnIntensityThreshold = 0.7f;
        [Tooltip("Probability per wave that an elite group spawns when intensity exceeds threshold.")]
        [Range(0f, 1f)] public float EliteSpawnChance = 0.4f;
        [Tooltip("Stat multiplier applied to elite enemies spawned by the Director.")]
        public float EliteStatMultiplier = 1.5f;

        [Header("Relief Events")]
        [Tooltip("Intensity threshold below which the Director may grant bonus resources.")]
        [Range(0f, 1f)] public float ReliefIntensityThreshold = 0.2f;
        [Tooltip("Bonus mana granted on a relief event.")]
        public int ReliefManaBonus = 40;
        [Tooltip("Bonus gold granted on a relief event.")]
        public int ReliefGoldBonus = 60;

        [Header("Pacing")]
        [Tooltip("Minimum seconds between any two Director-triggered events.")]
        public float MinEventCooldown = 20f;
        [Tooltip("If intensity stays above this for this many seconds, force a relief wave.")]
        [Range(0f, 1f)] public float SustainedHighIntensityThreshold = 0.85f;
        public float SustainedHighIntensityDuration = 30f;
    }
}
