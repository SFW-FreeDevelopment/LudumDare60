using System;
using System.Globalization;

namespace Balloonatic.Core
{
    public readonly record struct RestDeck(float X, float Altitude, float HalfWidth);
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
    /// A floating sky rock: parked at an X, bobbing on a triangle wave. Everything about its motion is
    /// a pure function of the tick — no integrator, no state — so the sim's collision check and the
    /// renderer's sprite land on the same pixel every frame, on both heads.
    /// </summary>
    public readonly struct RockDef
    {
        public readonly float X;
        public readonly float BaseAltitude;
        public readonly float BobAmplitude;
        public readonly int BobPeriodTicks;
        public readonly int BobPhaseTicks;

        public RockDef(float x, float baseAltitude, float bobAmplitude, int bobPeriodTicks, int bobPhaseTicks)
        {
            X = x;
            BaseAltitude = baseAltitude;
            BobAmplitude = Math.Max(0f, bobAmplitude);
            BobPeriodTicks = Math.Max(1, bobPeriodTicks);
            BobPhaseTicks = bobPhaseTicks;
        }

        /// <summary>Where the rock actually is this tick: a triangle bob, -amp .. +amp around the base.
        /// Straight lines, not a sine — the sim keeps its no-transcendentals rule.</summary>
        public float AltitudeAt(int tick)
        {
            if (BobAmplitude <= 0f)
                return BaseAltitude;
            int t = (tick + BobPhaseTicks) % BobPeriodTicks;
            if (t < 0)
                t += BobPeriodTicks;
            int half = Math.Max(1, BobPeriodTicks / 2);
            int u = t < half ? t : BobPeriodTicks - t;
            float triangle = 2f * u / half - 1f;   // -1 at t=0, +1 at the top, back down
            return BaseAltitude + BobAmplitude * triangle;
        }
    }

    /// <summary>
    /// A bird on an authored path: a polyline flown back and forth (ping-pong) at a constant speed.
    /// Its position at any tick is closed-form arithmetic on the tick — deterministic to the step,
    /// and the renderer draws exactly what the collision test felt.
    /// </summary>
    public readonly struct BirdDef
    {
        public readonly float[] WaypointX;
        public readonly float[] WaypointAltitude;
        public readonly float Speed;   // design px/s along the path

        readonly float[] _segmentEnds; // cumulative path length at the end of each segment
        readonly float _total;

        public BirdDef(float[] waypointX, float[] waypointAltitude, float speed)
        {
            if (waypointX == null || waypointAltitude == null || waypointX.Length != waypointAltitude.Length || waypointX.Length < 2)
                throw new ArgumentException("a bird path needs at least two waypoints");
            WaypointX = waypointX;
            WaypointAltitude = waypointAltitude;
            Speed = Math.Max(1f, speed);
            _segmentEnds = new float[waypointX.Length - 1];
            float d = 0f;
            for (int i = 0; i < _segmentEnds.Length; i++)
            {
                float dx = waypointX[i + 1] - waypointX[i];
                float da = waypointAltitude[i + 1] - waypointAltitude[i];
                d += (float)Math.Sqrt(dx * dx + da * da);   // once at load, never in a step
                _segmentEnds[i] = d;
            }
            _total = d;
        }

        /// <summary>Where the bird is this tick, plus the way it faces (+1 downwind / -1 home).
        /// Ping-pong keeps the route a simple authored line without a wrap teleport.</summary>
        public (float X, float Altitude, int Direction) At(int tick)
        {
            float travelled = tick / (float)Sim.StepHz * Speed;
            float s = travelled % (2f * _total);
            bool returning = s > _total;
            if (returning)
                s = 2f * _total - s;

            int seg = 0;
            while (seg < _segmentEnds.Length - 1 && s > _segmentEnds[seg])
                seg++;
            float from = seg == 0 ? 0f : _segmentEnds[seg - 1];
            float segLength = _segmentEnds[seg] - from;
            float u = segLength <= 0f ? 0f : (s - from) / segLength;

            float dxForward = WaypointX[seg + 1] - WaypointX[seg];
            float x = WaypointX[seg] + dxForward * u;
            float alt = WaypointAltitude[seg] + (WaypointAltitude[seg + 1] - WaypointAltitude[seg]) * u;
            int facing = (dxForward >= 0f ? 1 : -1) * (returning ? -1 : 1);
            return (x, alt, facing);
        }
    }

    /// <summary>
    /// A storm cloud bank: a box along the course that adds an oscillating gust on top of its band's
    /// wind. The gust alternates direction every <see cref="SimTuning.StormGustTicks"/> steps — the
    /// buffeting the player reads as the angry clouds — and Wind Assist removes it exactly like any
    /// other gust. The row of cloud sprites drawn across the box is the whole of its art.
    /// </summary>
    public readonly struct StormZone
    {
        public readonly float FromX;
        public readonly float ToX;
        public readonly float FromAltitude;
        public readonly float ToAltitude;
        public readonly float AddSpeed;   // signed: the mean push when the gust cycle averages out

        public StormZone(float fromX, float toX, float fromAltitude, float toAltitude, float addSpeed)
        {
            FromX = Math.Min(fromX, toX);
            ToX = Math.Max(fromX, toX);
            FromAltitude = Math.Min(fromAltitude, toAltitude);
            ToAltitude = Math.Max(fromAltitude, toAltitude);
            AddSpeed = addSpeed;
        }

        public bool Contains(float x, float altitude)
            => x >= FromX && x <= ToX && altitude >= FromAltitude && altitude < ToAltitude;
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

        /// <summary>The I9 hazards: floating rocks, birds on paths, storm banks. Authored data, and
        /// every position at a tick is a closed-form function of it — sim and renderer agree by
        /// construction, never by a shared mutable list.</summary>
        public RockDef[] Rocks { get; }
        public BirdDef[] Birds { get; }
        public StormZone[] Storms { get; }

        /// <summary>Design px of downwind travel the level expects; delivery targets sit inside it.</summary>
        public float CourseLength { get; }

        /// <summary>A good delivery beats this many seconds; it is one of the three rating stars.</summary>
        public int ParSeconds { get; }

        /// <summary>HUD-drawn form of the name, upper-cased once at construction: the draw loop must
        /// not allocate, or the garbage collector introduces the frame spikes the playtest felt.</summary>
        public string NameUpper { get; }
        public CargoKind Package { get; }
        public string PackageName { get; }
        public string DestinationName { get; }
        public string CollectObjective { get; }
        public string CarryObjective { get; }
        public string DeliveredMessage { get; }
        public string RequestLabel { get; }
        public RestDeck[] Platforms { get; }

        public LevelDef(string id, string name, float worldHeight, WindBand[] bands, GustZone[] gusts, float courseLength, int parSeconds,
                        RockDef[]? rocks = null, BirdDef[]? birds = null, StormZone[]? storms = null, CargoKind package = CargoKind.Teddy, RestDeck[]? platforms = null)
        {
            Id = id;
            Name = name;
            NameUpper = name.ToUpperInvariant();
            Package = package;
            Platforms = platforms ?? Array.Empty<RestDeck>();
            PackageName = package == CargoKind.Bottle ? "BOTTLE" : package == CargoKind.Letter ? "LETTER" : "TEDDY";
            DestinationName = id == "nursery-skies" ? "NURSERY" : id == "winding-trail" ? "TOY ROOM" : id == "night-crossing" ? "GARDEN" : "COTTAGE";
            CollectObjective = "PICK UP " + PackageName;
            CarryObjective = PackageName + " > " + DestinationName;
            DeliveredMessage = package==CargoKind.Bottle ? "MILK HAS ARRIVED!" : package==CargoKind.Letter ? "LETTER DELIVERED!" : "TEDDY IS HOME!";
            RequestLabel = PackageName + "?";
            WorldHeight = worldHeight;
            Bands = bands;
            Gusts = gusts ?? Array.Empty<GustZone>();
            Rocks = rocks ?? Array.Empty<RockDef>();
            Birds = birds ?? Array.Empty<BirdDef>();
            Storms = storms ?? Array.Empty<StormZone>();
            CourseLength = courseLength;
            ParSeconds = parSeconds;
        }

        /// <summary>Centre tests for pickup/authoring; physical surface contacts use the basket footprint.</summary>
        public bool OverLaunchDeck(float x) => Math.Abs(x) <= SimTuning.DeckHalfWidth;
        public bool OverDeliveryDeck(float x) => Math.Abs(x - CourseLength) <= SimTuning.DeckHalfWidth;

        /// <summary>
        /// Design px/s of band wind by strength. Three readable steps, no more. Tuned down twice by
        /// the I5 feel playtest (40/75/110 -> 20/38/55 -> these): the whole world sweeps past the
        /// camera at these speeds, and the old numbers made the sky fly at the player (motion
        /// sickness on record; round 2 said "a little too fast"). The ghost pilot still lands three
        /// stars inside par on every level at these speeds — that test licenses them.
        /// </summary>
        public static float WindSpeed(int strength) => strength switch
        {
            <= 1 => 15f,
            2 => 28f,
            _ => 40f,
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

        /// <summary>
        /// The storm buffet at a place and tick: the sum over every bank containing that point of a
        /// square-wave gust — two periods with, two against. Deterministic from the tick alone, so a
        /// replay is buffeted identically and the renderer's cloud chevrons point exactly where the
        /// air is pushing.
        /// </summary>
        public float StormWindAt(float x, float altitude, int tick)
        {
            float add = 0f;
            for (int i = 0; i < Storms.Length; i++)
            {
                if (!Storms[i].Contains(x, altitude))
                    continue;
                int phase = (tick / SimTuning.StormGustTicks + i * 3) % 4;
                add += phase < 2 ? Storms[i].AddSpeed : -Storms[i].AddSpeed;
            }
            return add;
        }

        /// <summary>Where a numbered rock/bird is this tick — the one source both the collision test
        /// and the sprite draw call, so physics and pixels cannot drift apart.</summary>
        public (float X, float Altitude) RockAt(int index, int tick)
        {
            RockDef rock = Rocks[index];
            return (rock.X, rock.AltitudeAt(tick));
        }

        public (float X, float Altitude, int Direction) BirdAt(int index, int tick) => Birds[index].At(tick);
    }

    /// <summary>
    /// The authored levels. Three are enough for the jam scope (PLAN.md); each ships with the autopilot
    /// solvability test in <c>WindLayerTests</c>. Ids are the harness's handles and stay frozen
    /// (<c>winding-trail</c> is Toy Isles, <c>night-crossing</c> is Cloud Garden — the display names
    /// followed PLAN, the ids did not, so committed tests and screenshots keep their anchors).
    /// </summary>
    public static class LevelCatalog
    {
        public const string DefaultId = "nursery-skies";

        public static readonly LevelDef[] Levels =
        {
            // Tutorial: the low and middle air both carry toward the islands, the top air blows you home.
            // The rocks sit in the homeward air, where a player who overshoots the deck and climbs into
            // the return band must learn to watch the ceiling, and one pebble hovers over the launch
            // grass for a ground-scaper. The teaching line itself stays empty; the ghost flies it.
            new LevelDef(
                "nursery-skies", "Nursery Skies", 400f,
                new[]
                {
                    new WindBand(0f, 120f, +1, 1),
                    new WindBand(120f, 260f, +1, 2),
                    new WindBand(260f, 400f, -1, 2),
                },
                Array.Empty<GustZone>(),
                600f, 45,
                rocks: new[]
                {
                    new RockDef(300f, 320f, 8f, 240, 0),
                    new RockDef(540f, 340f, 8f, 300, 90),
                    new RockDef(200f, 345f, 4f, 240, 40),
                }, package: CargoKind.Bottle, platforms: new[] { new RestDeck(220f, 120f, 36f) }),

            // Toy Isles, a zigzag: the middle air sprints, the upper air rebukes you, and the low air —
            // the air a landing is made from — carries gently home. The bird works the mid sprint and
            // the rocks guard the return and ceiling air: a player cruising fast, or fishing for home
            // wind up high, shares that sky with them. The low approach stays clear for a landing
            // descent. Opposing bands are the lesson; the hazards are seasoning.
            new LevelDef(
                "winding-trail", "Toy Isles", 520f,
                new[]
                {
                    new WindBand(0f, 110f, -1, 1),
                    new WindBand(110f, 240f, +1, 2),
                    new WindBand(240f, 360f, -1, 2),
                    new WindBand(360f, 520f, +1, 3),
                },
                new[] { new GustZone(240f, 300f, -1, 14f) },
                700f, 60,
                rocks: new[]
                {
                    new RockDef(560f, 300f, 8f, 280, 70),
                    new RockDef(640f, 430f, 8f, 260, 90),
                },
                birds: new[]
                {
                    new BirdDef(new[] { 440f, 520f }, new[] { 280f, 240f }, 40f),
                }, platforms: new[] { new RestDeck(200f,165f,36f) }),

            // Cloud Garden, the long haul: a fast ceiling over a calm floor. The middle air barely
            // carries and the air at the ceiling is the only real pace — go up to cross, come down to
            // land, and never try to land in the ceiling's hurry. A storm bank waits in the contrary
            // air mid-course, the bird patrols the quiet middle near home, and the rocks sit high —
            // well clear of the long low drift the landing is flown from. Strong wind plus hazards is
            // the lesson.
            new LevelDef(
                "night-crossing", "Cloud Garden", 600f,
                new[]
                {
                    new WindBand(0f, 140f, -1, 1),
                    new WindBand(140f, 300f, +1, 2),
                    new WindBand(300f, 460f, -1, 1),
                    new WindBand(460f, 600f, +1, 3),
                },
                new[] { new GustZone(460f, 540f, +1, 15f) },
                900f, 80,
                rocks: new[]
                {
                    new RockDef(420f, 350f, 8f, 240, 40),
                    new RockDef(760f, 480f, 6f, 280, 130),
                },
                birds: new[]
                {
                    new BirdDef(new[] { 250f, 320f }, new[] { 260f, 220f }, 30f),
                },
                storms: new[]
                {
                    new StormZone(380f, 500f, 210f, 236f, 12f),
                }, package: CargoKind.Letter, platforms: new[] {new RestDeck(280f,170f,36f),new RestDeck(650f,170f,36f)}),
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
