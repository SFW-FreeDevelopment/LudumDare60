using System;
using System.Globalization;

namespace Balloonatic.Core
{
    /// <summary>
    /// One altitude band of wind: the only steering the player has. Bands are authored per level, so a
    /// level's puzzle is "which heights carry me where".
    /// </summary>
    public readonly struct WindBand
    {
        public readonly float FromAltitude;
        public readonly float ToAltitude;

        /// <summary>+1 blows toward increasing X (downwind), -1 toward decreasing X.</summary>
        public readonly int Direction;

        /// <summary>1..3 — how hard that band blows, mapped through <see cref="LevelDef.WindSpeed"/>.</summary>
        public readonly int Strength;

        public WindBand(float fromAltitude, float toAltitude, int direction, int strength)
        {
            FromAltitude = fromAltitude;
            ToAltitude = toAltitude;
            Direction = direction >= 0 ? 1 : -1;
            Strength = Math.Clamp(strength, 1, 3);
        }

        public bool Contains(float altitude) => altitude >= FromAltitude && altitude < ToAltitude;
    }

    /// <summary>
    /// A gust zone: a region that adds wind on top of its band. The hook I3 needs; authored zones arrive
    /// with level content in I10.
    /// </summary>
    public readonly struct GustZone
    {
        public readonly float FromAltitude;
        public readonly float ToAltitude;
        public readonly int Direction;
        public readonly float AddSpeed;

        public GustZone(float fromAltitude, float toAltitude, int direction, float addSpeed)
        {
            FromAltitude = fromAltitude;
            ToAltitude = toAltitude;
            Direction = direction >= 0 ? 1 : -1;
            AddSpeed = Math.Max(0f, addSpeed);
        }

        public bool Contains(float altitude) => altitude >= FromAltitude && altitude < ToAltitude;
    }

    /// <summary>
    /// One authored level: its wind bands, its gust zones, and how tall its world is. Levels are data, so
    /// a solvability test can be generated per level and no level can ever ship unsolvable (PLAN.md 10).
    /// </summary>
    public sealed class LevelDef
    {
        public string Id { get; }
        public string Name { get; }
        public float WorldHeight { get; }
        public WindBand[] Bands { get; }
        public GustZone[] Gusts { get; }

        /// <summary>Design px of downwind travel the level expects; delivery targets sit inside it.</summary>
        public float CourseLength { get; }

        /// <summary>A good delivery beats this many seconds; it is one of the three rating stars.</summary>
        public int ParSeconds { get; }

        public LevelDef(string id, string name, float worldHeight, WindBand[] bands, GustZone[] gusts, float courseLength, int parSeconds)
        {
            Id = id;
            Name = name;
            WorldHeight = worldHeight;
            Bands = bands;
            Gusts = gusts ?? Array.Empty<GustZone>();
            CourseLength = courseLength;
            ParSeconds = parSeconds;
        }

        /// <summary>Deck landing test: the basket centre over a deck of this level, launch or delivery.</summary>
        public bool OverLaunchDeck(float x) => Math.Abs(x) <= SimTuning.DeckHalfWidth;
        public bool OverDeliveryDeck(float x) => Math.Abs(x - CourseLength) <= SimTuning.DeckHalfWidth;

        /// <summary>Design px/s of band wind by strength. Three readable steps, no more.</summary>
        public static float WindSpeed(int strength) => strength switch
        {
            <= 1 => 40f,
            2 => 75f,
            _ => 110f,
        };

        public WindBand BandAt(float altitude)
        {
            foreach (WindBand band in Bands)
                if (band.Contains(altitude))
                    return band;
            // Above the authored bands the air keeps the top band's mind: no silent calm layer where a
            // overcooked balloon forgets which way it is going.
            return Bands.Length > 0 ? Bands[Bands.Length - 1] : default;
        }

        /// <summary>
        /// Signed wind at one altitude: band wind plus any gust on top of it. This is the whole of the
        /// horizontal model — the player changes altitude, the sky decides the rest.
        /// </summary>
        public float WindAt(float altitude)
        {
            WindBand band = BandAt(altitude);
            float speed = band.Direction * WindSpeed(band.Strength);
            foreach (GustZone gust in Gusts)
                if (gust.Contains(altitude))
                    speed += gust.Direction * gust.AddSpeed;
            return speed;
        }
    }

    /// <summary>
    /// The authored levels. Three are enough for the jam scope (PLAN.md); each ships with the autopilot
    /// solvability test in <c>WindLayerTests</c>.
    /// </summary>
    public static class LevelCatalog
    {
        public const string DefaultId = "nursery-skies";

        public static readonly LevelDef[] Levels =
        {
            // Tutorial: the low and middle air both carry toward the islands, the top air blows you home.
            new LevelDef(
                "nursery-skies", "Nursery Skies", 400f,
                new[]
                {
                    new WindBand(0f, 120f, +1, 1),
                    new WindBand(120f, 260f, +1, 2),
                    new WindBand(260f, 400f, -1, 2),
                },
                Array.Empty<GustZone>(),
                600f, 45),

            // A zigzag: the middle air sprints, the upper air rebukes you, and the low air — the air a
            // landing is made from — carries gently home. The course is a deliberate climb and a patient drop.
            new LevelDef(
                "winding-trail", "Winding Trail", 520f,
                new[]
                {
                    new WindBand(0f, 110f, +1, 1),
                    new WindBand(110f, 240f, +1, 2),
                    new WindBand(240f, 360f, -1, 2),
                    new WindBand(360f, 520f, +1, 3),
                },
                new[] { new GustZone(240f, 300f, -1, 35f) },
                700f, 60),

            // Night: a long haul under a fast ceiling, above a calm floor. The middle air barely
            // carries and the air at the ceiling is the only real pace — go up to cross, come down to
            // land, and never try to land in the ceiling's hurry.
            new LevelDef(
                "night-crossing", "Night Crossing", 600f,
                new[]
                {
                    new WindBand(0f, 140f, +1, 1),
                    new WindBand(140f, 300f, +1, 1),
                    new WindBand(300f, 460f, -1, 1),
                    new WindBand(460f, 600f, +1, 2),
                },
                new[] { new GustZone(460f, 540f, +1, 40f) },
                900f, 80),
        };

        public static bool TryGet(string? id, out LevelDef level)
        {
            foreach (LevelDef candidate in Levels)
            {
                if (string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    level = candidate;
                    return true;
                }
            }

            level = Levels[0];
            return false;
        }

        public static LevelDef Get(string? id)
        {
            TryGet(id, out LevelDef level);
            return level;
        }
    }
}
