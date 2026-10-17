using System;
using System.Collections.Generic;
using Balloonatic.Core;
using FluentAssertions;
using Xunit;

namespace Balloonatic.Tests
{
    /// <summary>
    /// I9 hazards: rocks, birds, storms, and the hearts that pay for meeting them. Every position is
    /// a closed form on the tick, so these tests stand the balloon in front of a hazard and read the
    /// outcome exactly — no timing, no tolerance beyond physics.
    /// </summary>
    public class HazardTests
    {
        static SimState Vulnerable(string levelId, float x, float altitude)
        {
            SimState state = new SimState { Seed = 0x1919, LevelId = levelId };
            state.X = x;
            state.Altitude = altitude;
            state.HurtTick = -SimTuning.InvulnTicks;   // stand on the hazard with no grace
            return state;
        }

        [Fact]
        public void RockCostsOneHeartAndThrowsTheBalloonAway()
        {
            LevelDef level = LevelCatalog.Get("nursery-skies");
            float rockAlt = level.RockAt(0, 1).Altitude;   // the tick the step will evaluate

            // A side hit from slightly above: body centre 20 px above the rock, 10 px downwind — the
            // knockback's sign has to come from where we stood, not from one step of sinking.
            SimState state = Vulnerable("nursery-skies", x: 310f, altitude: rockAlt - SimTuning.BalloonBodyOffset + 20f);
            SimRules.Step(state, SimInput.None);

            state.Hearts.Should().Be(SimTuning.HeartsStart - 1);
            state.HurtTick.Should().Be(state.Tick);
            state.VelocityX.Should().BeApproximately(SimTuning.KnockbackX, 2f);   // pushed off, not parked
            state.VelocityY.Should().BeGreaterThanOrEqualTo(SimTuning.KnockbackY - 3f);
            state.Complete.Should().BeFalse();                    // two hearts left: still flying
        }

        [Fact]
        public void SecondContactInsideTheInvulnerabilityWindowIsFree()
        {
            LevelDef level = LevelCatalog.Get("nursery-skies");
            SimState state = Vulnerable("nursery-skies", x: 300f, altitude: level.RockAt(0, 1).Altitude - SimTuning.BalloonBodyOffset);

            for (int i = 0; i < 30; i++)                          // sit on the rock for half a second
                SimRules.Step(state, SimInput.None);

            state.Hearts.Should().Be(SimTuning.HeartsStart - 1);   // one hit, not thirty
            state.Tick.Should().Be(30);
        }

        [Fact]
        public void LastHeartEndsTheRunWhereItStands()
        {
            LevelDef level = LevelCatalog.Get("nursery-skies");
            SimState state = new SimState { Seed = 0x1919, LevelId = "nursery-skies" };

            for (int hit = 0; hit < SimTuning.HeartsStart; hit++)
            {
                float rockAlt = level.RockAt(0, state.Tick + 1).Altitude;
                state.X = 300f;
                state.Altitude = rockAlt - SimTuning.BalloonBodyOffset;
                state.VelocityX = 0f;
                state.VelocityY = 0f;
                state.HurtTick = -SimTuning.InvulnTicks;
                SimRules.Step(state, SimInput.None);
            }

            state.Hearts.Should().Be(0);
            state.Complete.Should().BeTrue();
            state.Delivered.Should().BeFalse();
            state.CompletedAtTick.Should().Be(state.Tick);
        }

        [Fact]
        public void RockBobStaysInItsLaneAndBirdFliesItsPathDeterministically()
        {
            LevelDef level = LevelCatalog.Get("winding-trail");

            for (int t = 0; t < 1200; t += 37)
            {
                (float x, float alt) = level.RockAt(0, t);
                x.Should().Be(560f);                                          // rocks hold their X
                alt.Should().BeInRange(300f - 8f - 0.001f, 300f + 8f + 0.001f);
                level.RockAt(0, t).Altitude.Should().Be(alt);                 // same tick, same answer
            }

            LevelDef night = LevelCatalog.Get("night-crossing");
            (float bx0, float ba0, int dir0) = night.BirdAt(0, 0);
            bx0.Should().Be(250f);                                            // starts at its first waypoint
            ba0.Should().Be(260f);
            (float bx1, float ba1, _) = night.BirdAt(0, 120);
            bx1.Should().NotBe(bx0);                                          // two seconds on, it moved

            // Ping-pong period: there and back at 30 px/s over ~80 px of path.
            int period = (int)Math.Ceiling(2f * 81f / 30f * Sim.StepHz);
            night.BirdAt(0, period).X.Should().BeApproximately(bx0, 1.5f);    // home again, near enough
            dir0.Should().Be(1);                                              // heading downwind out of the gate
        }

        [Fact]
        public void BirdOnItsPathCostsAHeart()
        {
            LevelDef level = LevelCatalog.Get("night-crossing");
            (float bx, float ba, int _) = level.BirdAt(0, 200);

            SimState state = Vulnerable("night-crossing", bx, ba - SimTuning.BalloonBodyOffset);
            state.Tick = 199;                             // one step and the bird is exactly where we stood
            SimRules.Step(state, SimInput.None);

            state.Hearts.Should().Be(SimTuning.HeartsStart - 1);
        }

        [Fact]
        public void StormBankBuffetsAndWindAssistTakesItAway()
        {
            // Night Crossing's bank sits mid the contrary air (380..500 x, 335..365 alt). Park a
            // balloon dead in it and hold the place: without assist the gust swings it about the
            // band wind; with assist the air is the band, flat as a line.
            float bandWind = -LevelDef.WindSpeed(1);
            SimState raw = HoldInStorm(assist: false, out List<float> rawVx);
            SimState calm = HoldInStorm(assist: true, out List<float> calmVx);

            raw.VelocityX.Should().NotBe(calm.VelocityX);
            float rawSpread = MaxAbs(rawVx, bandWind);
            float calmSpread = MaxAbs(calmVx, bandWind);
            rawSpread.Should().BeGreaterThan(4f);         // the buffet bites
            calmSpread.Should().BeLessThan(2f);           // assist smoothed it out entirely
        }

        static SimState HoldInStorm(bool assist, out List<float> vxTrack)
        {
            SimState state = new SimState { Seed = 0x1919, LevelId = "night-crossing" };
            state.WindAssist = assist;
            vxTrack = new List<float>();
            for (int i = 0; i < 240; i++)
            {
                SimRules.Step(state, SimInput.None);
                state.X = 440f;       // hold the place inside the bank — this test is about the gust,
                state.Altitude = 350f;    // and drift or sink would carry it out mid-swing
                state.VelocityY = 0f;
                if (i >= 120)         // only after the initial settle; the first second always swings to the band
                    vxTrack.Add(state.VelocityX);
            }
            return state;
        }

        static float MaxAbs(List<float> values, float around)
        {
            float worst = 0f;
            foreach (float v in values)
                worst = Math.Max(worst, Math.Abs(v - around));
            return worst;
        }

        [Fact]
        public void GhostPilotFliesEveryCourseWithoutScratches()
        {
            // Solvability is the wind's promise; dodging is the player's job, so the Autopilot steps
            // with hazards off. This guards the wiring: if a real-damage step ever gets stitched back
            // into the ghost, an authored rock on a delivery line makes it red.
            foreach (LevelDef level in LevelCatalog.Levels)
            {
                SimState state = new SimState { Seed = 0x9E37u + (uint)level.Id.Length, LevelId = level.Id };
                bool delivered = Autopilot.Deliver(state, level, 120f);
                delivered.Should().BeTrue($"because {level.Id} must stay solvable");
                state.Hearts.Should().Be(SimTuning.HeartsStart, $"because {level.Id}'s flown line should miss its hazards");
            }
        }

        [Fact]
        public void HarnessMirrorsHeartsAndWarpsOntoARock()
        {
            TestHarness harness = new TestHarness();
            harness.Handle("reset", "1234");

            string hazards = harness.Handle("hazards", "");
            hazards.Should().StartWith("rock:");
            string firstRock = hazards.Split(';')[0];
            string[] xy = firstRock.Substring("rock:".Length).Split(',');
            float rockX = float.Parse(xy[0], System.Globalization.CultureInfo.InvariantCulture);
            float rockAlt = float.Parse(xy[1], System.Globalization.CultureInfo.InvariantCulture);

            harness.Handle("warp", $"{rockX.ToString(System.Globalization.CultureInfo.InvariantCulture)}={rockAlt - SimTuning.BalloonBodyOffset}");
            harness.Handle("step", "2");

            harness.Handle("hearts", "").Should().Be((SimTuning.HeartsStart - 1).ToString());
            harness.Handle("state", "").Should().Contain("\"hearts\":2");
        }
    }
}
