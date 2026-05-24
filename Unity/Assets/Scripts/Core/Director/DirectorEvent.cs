namespace DragonTD.Core
{
    public enum DirectorEventType
    {
        EliteEnemySpawn,
        BonusResourceDrop,
        ReliefWave,
        IntensityPeak,
        DifficultyIncreased,
        DifficultyDecreased
    }

    public class DirectorEvent
    {
        public DirectorEventType Type { get; }
        public float IntensityAtTrigger { get; }
        public float DifficultyMultiplierAtTrigger { get; }
        public int WaveIndex { get; }

        public DirectorEvent(DirectorEventType type, float intensity, float difficulty, int wave)
        {
            Type = type;
            IntensityAtTrigger = intensity;
            DifficultyMultiplierAtTrigger = difficulty;
            WaveIndex = wave;
        }

        public override string ToString() =>
            $"[Director] {Type} | Wave {WaveIndex} | Intensity {IntensityAtTrigger:F2} | Difficulty {DifficultyMultiplierAtTrigger:F2}x";
    }
}
