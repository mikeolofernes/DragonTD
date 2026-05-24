namespace DragonTD.Core
{
    // Tracks and smooths the battle intensity value (0–1).
    // Intensity rises while enemies are alive, spikes on life loss, and decays on wave clear.
    public class IntensityMeter
    {
        private float _value;
        private readonly GameDirectorConfig _config;

        public float Value => _value;
        public bool IsHigh => _value >= _config.EliteSpawnIntensityThreshold;
        public bool IsLow  => _value <= _config.ReliefIntensityThreshold;

        public IntensityMeter(GameDirectorConfig config) => _config = config;

        public void Tick(float deltaTime, bool enemiesActive)
        {
            if (enemiesActive)
                _value = System.Math.Min(1f, _value + _config.IntensityRiseRate * deltaTime);
            else
                _value = System.Math.Max(0f, _value - _config.IntensityDecayRate * deltaTime);
        }

        public void Spike(float amount) =>
            _value = System.Math.Min(1f, _value + amount);

        public void Drop(float amount) =>
            _value = System.Math.Max(0f, _value - amount);

        public void Reset() => _value = 0f;
    }
}
