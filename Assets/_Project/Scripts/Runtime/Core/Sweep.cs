namespace PromVR.MaterialAccumulation.Core
{
    public readonly struct Sweep
    {
        public Sweep(
            float startX,
            float startZ,
            float endX,
            float endZ,
            float startRadius,
            float endRadius,
            float durationSeconds)
        {
            StartX = startX;
            StartZ = startZ;
            EndX = endX;
            EndZ = endZ;
            StartRadius = startRadius;
            EndRadius = endRadius;
            DurationSeconds = durationSeconds;
        }

        public float StartX { get; }

        public float StartZ { get; }

        public float EndX { get; }

        public float EndZ { get; }

        public float StartRadius { get; }

        public float EndRadius { get; }

        public float DurationSeconds { get; }
    }
}
