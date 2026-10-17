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

        /// <summary>
        /// Descent speed at or below which touching a deck counts as a soft landing — the whole landing
        /// skill in one number. It is slower than terminal descent (~25 px/s), so arriving soft means
        /// having closed the burner early and let the envelope bleed heat on approach.
        /// </summary>
        public const float SoftLandingVy = 18f;

        /// <summary>Surface height of launch and delivery decks, design px above the ground plane.</summary>
        public const float DeckTopAltitude = 38f;

        /// <summary>
        /// Half-width of a landing deck; the basket must come down inside it. Sized against a landing
        /// read: a settled approach drifts up to ~40 px while the last 50 px of altitude bleed away, so
        /// a narrower deck punishes arrivals that are flown correctly and only miss by physics the
        /// player cannot see. 56 is "wide road, small prize" — the deck reads at design scale and a
        /// human (and the autopilot) can actually land on it.
        /// </summary>
        public const float DeckHalfWidth = 56f;

        /// <summary>Share of descent speed returned as bounce when a landing is too fast.</summary>
        public const float BounceRestitution = 0.4f;

        /// <summary>Steps a "too fast" reason tag stays up after a bounce (1.5 s of reason).</summary>
        public const int BounceReasonSteps = 90;

        // ---- I9 hazards: hearts, and what getting hit does ----

        /// <summary>Hearts at launch. Losing the last one ends the run where it stands.</summary>
        public const int HeartsStart = 3;

        /// <summary>Steps of grace after a hit (2 s): one collision is one heart, never a chip chain.
        /// A fresh launch sits inside this window too — the first two seconds of a course cannot hurt.</summary>
        public const int InvulnTicks = 120;

        /// <summary>Horizontal and vertical impulse (design px/s) thrown away from the hazard that hit.</summary>
        public const float KnockbackX = 48f;
        public const float KnockbackY = 30f;

        /// <summary>Steps the camera shake holds after a hit. Short and one pixel: the shake is a
        /// bruise, not an earthquake — fast background motion is on the motion-sickness record.</summary>
        public const int ShakeTicks = 30;

        /// <summary>The envelope-and-basket collision box, as measured off the sheet sprite: the body
        /// lives from ~4 px below the altitude line to ~72 px above it, 60 design px wide.</summary>
        public const float BalloonBodyOffset = 34f;   // body centre, above the basket line
        public const float BalloonHalfWidth = 26f;
        public const float BalloonHalfHeight = 38f;

        /// <summary>Half-extents of the sheet rock and bird sprites (slice px / 2 = design px).</summary>
        public const float RockHalfWidth = 10f;
        public const float RockHalfHeight = 8f;
        public const float BirdHalfWidth = 6f;
        public const float BirdHalfHeight = 4f;

        /// <summary>Steps each half of a storm gust's cycle lasts: back, back, forth, forth.</summary>
        public const int StormGustTicks = 18;

        // Terminal rise at pegged heat with no ballast: (LiftGain - Gravity) / Drag ≈ 85 px/s, so one
        // 120 px wind band is crossed in well under two seconds while a coast down takes several. Those
        // two numbers — snappy up, patient down — are the feel gate's first subject in I5.
    }
}
