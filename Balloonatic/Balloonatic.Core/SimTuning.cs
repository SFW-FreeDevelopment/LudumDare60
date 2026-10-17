namespace Balloonatic.Core
{
    /// <summary>
    /// Every simulation tunable in one place, because "how does the balloon feel" must be answerable by
    /// reading one file (AGENTS.md: tunables live in one config object). Units are design pixels and
    /// seconds; the step is fixed at 60 Hz so all of these are frame-rate independent by construction.
    ///
    /// The model below is deliberately physical, not arcade: the balloon has weight, the burner has
    /// thermal lag, and the envelope has drag. That is what makes reading a wind band worth doing — the
    /// craft has to be *flown*, and momentum overshoot is a feature the tests pin.
    /// </summary>
    public static class SimTuning
    {
        /// <summary>Temperature the air alone would settle the envelope to, 0..1.</summary>
        public const float TempAmbient = 0.30f;

        /// <summary>Fully open burner ceiling. Above this the gauge is pegged, not the physics.</summary>
        public const float TempMax = 1.00f;

        /// <summary>Temperature gained per second of held heat: ~3 s from ambient to pegged.</summary>
        public const float HeatRate = 0.22f;

        /// <summary>Base cooling per second toward ambient — the envelope always leaks heat.</summary>
        public const float CoolBase = 0.06f;

        /// <summary>Extra cooling proportional to how far above ambient the envelope is: hot air escapes faster.</summary>
        public const float CoolProportional = 0.20f;

        /// <summary>Upward acceleration per unit of temperature. At pegged heat with no ballast this is the rise ceiling.</summary>
        public const float LiftGain = 210f;

        /// <summary>Constant downward acceleration of envelope, basket, Charlotte and the bunny.</summary>
        public const float Gravity = 62f;

        /// <summary>Downward acceleration per sandbag still aboard. Two of these is the level's ballast.</summary>
        public const float BallastPerBag = 22f;

        /// <summary>Sandbags per level, non-recoverable once dropped (docs/PLAN.md decision 8).</summary>
        public const int BagCapacity = 2;

        /// <summary>Vertical drag, per second. Sets terminal velocity: net acceleration divided by this.</summary>
        public const float Drag = 1.75f;

        /// <summary>Altitude of the ground plane. The balloon never goes below it.</summary>
        public const float GroundAltitude = 0f;

        /// <summary>
        /// How fast horizontal velocity chases the band's wind, per second. A first-order lag with a time
        /// constant of half a second: crossing a boundary between +75 and -110 px/s takes over a second to
        /// settle, which is the difference between "the wind caught me" and "the teleporter moved me".
        /// </summary>
        public const float WindFollowRate = 2.0f;

        // Terminal rise at pegged heat with no ballast: (LiftGain - Gravity) / Drag ≈ 85 px/s, so one
        // 120 px wind band is crossed in well under two seconds while a coast down takes several. Those
        // two numbers — snappy up, patient down — are the feel gate's first subject in I5.
    }
}
