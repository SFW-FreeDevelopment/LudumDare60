using Balloonatic.Core;
using FluentAssertions;
using Xunit;

namespace Balloonatic.Tests
{
    /// <summary>
    /// The wind is the steering, so these are handling tests. They pin the three properties the game's
    /// readability depends on: the band you are in owns your horizontal velocity, crossing a boundary is
    /// a settle and not a teleport, and every authored level is winnable by altitude choice alone.
    /// </summary>
    public class WindLayerTests
    {
        static float F(TestHarness h, string call) => float.Parse(h.Handle(call, ""), System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>Heats to a target altitude and holds it there with the autopilot, so the test is about wind.</summary>
        static void HoldAt(TestHarness h, LevelDef level, float target, int steps)
        {
            for (int i = 0; i < steps; i++)
                SimRules.Step(h.State, Autopilot.TowardAltitude(h.State, target), level);
        }

        [Fact]
        public void The_band_you_are_in_owns_your_horizontal_velocity()
        {
            LevelDef level = LevelCatalog.Get("nursery-skies");
            TestHarness h = new TestHarness();
            h.Handle("reset", "1234");

            HoldAt(h, level, 190f, 600);     // middle band, + direction, strength 2

            System.Math.Abs(F(h, "velocity")).Should().BeLessThan(40f);   // sanity: holding altitude
            F(h, "x").Should().BeGreaterThan(100f);                      // and travelling downwind

            float settled = F(h, "velocityX");
            settled.Should().BeApproximately(LevelDef.WindSpeed(2), 3f);   // chases the band's wind
            h.State.BallastHeld.Should().Be(SimTuning.BagCapacity);       // ...without touching ballast
        }

        [Fact]
        public void A_higher_band_blows_the_other_way()
        {
            LevelDef level = LevelCatalog.Get("nursery-skies");
            TestHarness h = new TestHarness();
            h.Handle("reset", "1234");

            HoldAt(h, level, 190f, 300);     // drift out with the middle band
            float drifting = F(h, "velocityX");

            HoldAt(h, level, 330f, 1020);    // climb into the top band, - direction (the hold is gentle)

            drifting.Should().BePositive();
            F(h, "velocityX").Should().BeNegative();        // the same craft, blown home
        }

        [Fact]
        public void Crossing_a_band_boundary_is_a_settle_not_a_snap()
        {
            LevelDef level = LevelCatalog.Get("nursery-skies");
            TestHarness h = new TestHarness();
            h.Handle("reset", "1234");

            // Climb through the reversal boundary (120 up, 260 into the top band) one step at a time and
            // watch what horizontal velocity is asked to do on the exact step the crossing happens.
            float crossingDelta = 0f;
            bool crossed = false;
            for (int i = 0; i < 3000 && !crossed; i++)
            {
                float velocityBefore = h.State.VelocityX;
                float altitudeBefore = h.State.Altitude;
                SimRules.Step(h.State, Autopilot.TowardAltitude(h.State, 330f), level);

                if (altitudeBefore < 260f && h.State.Altitude >= 260f)
                {
                    crossingDelta = System.Math.Abs(h.State.VelocityX - velocityBefore);
                    crossed = true;
                }
            }

            Assert.True(crossed, "the balloon never reached the boundary; retune the target altitude");

            // The wind follows with a half-second time constant, so the crossing step itself moves
            // velocity by a few px/s. A model that snapped to the new band would change it by ~150 here.
            crossingDelta.Should().BeInRange(0f, 8f);

            // And the settle takes visible time: well over half a second to reach the new band's wind.
            int settleSteps = 0;
            float targetWind = level.BandAt(330f).Direction * LevelDef.WindSpeed(2);
            while (settleSteps < 300 && System.Math.Abs(h.State.VelocityX - targetWind) > 0.1f * LevelDef.WindSpeed(2))
            {
                SimRules.Step(h.State, Autopilot.TowardAltitude(h.State, 330f), level);
                settleSteps++;
            }
            settleSteps.Should().BeGreaterThan(30).And.BeLessThan(300);
        }

        [Fact]
        public void A_gust_zone_adds_wind_on_top_of_its_band()
        {
            LevelDef level = LevelCatalog.Get("winding-trail");   // gust -35 inside 240..300

            // Data-level: the wind function must include the gust. The flight-level consequence is covered
            // by the solvability tests and by the autopilot holding a band that gusts.
            level.WindAt(270f).Should().Be(level.BandAt(270f).Direction * LevelDef.WindSpeed(2) - 35f);
            level.WindAt(330f).Should().Be(-LevelDef.WindSpeed(2));

            // And the sim obeys the data: a balloon held inside a gust drifts upwind faster than one held
            // in the same band without it.
            TestHarness inGust = new TestHarness();
            inGust.Handle("reset", "1234");
            inGust.Handle("level", "winding-trail");
            HoldAt(inGust, level, 265f, 1020);

            TestHarness outside = new TestHarness();
            outside.Handle("reset", "1234");
            outside.Handle("level", "winding-trail");
            HoldAt(outside, level, 330f, 1020);

            F(inGust, "velocityX").Should().BeLessThan(F(outside, "velocityX") - 20f);
        }

        [Theory]
        [InlineData("nursery-skies")]
        [InlineData("winding-trail")]
        [InlineData("night-crossing")]
        public void Every_authored_level_is_solvable_by_altitude_choice_alone(string levelId)
        {
            LevelDef level = LevelCatalog.Get(levelId);
            SimState state = new SimState { Seed = 0x5EED, LevelId = levelId };

            float reached = Autopilot.FlyDownwind(state, level, 90f);

            reached.Should().BeGreaterThanOrEqualTo(level.CourseLength);
            state.X.Should().BeGreaterThanOrEqualTo(level.CourseLength);
        }

        [Fact]
        public void The_bridge_can_switch_levels_and_relaunches_on_switch()
        {
            TestHarness h = new TestHarness();
            h.Handle("reset", "1234");
            h.Handle("level", "").Should().Be(LevelCatalog.DefaultId);

            h.Handle("set", "heat=1");
            h.Handle("step", "300");
            F(h, "x").Should().BeGreaterThan(0f);

            h.Handle("level", "night-crossing").Should().Be("ok:level=night-crossing");
            h.Handle("level", "").Should().Be("night-crossing");
            F(h, "x").Should().Be(0f);                     // switched = relaunched
            F(h, "altitude").Should().Be(SimTuning.DeckTopAltitude);   // …back on the launch deck

            h.Handle("level", "nowhere-level").Should().Be("error:unknown-level:nowhere-level");
            h.Handle("level", "").Should().Be("night-crossing");   // a bad name changes nothing
        }
    }
}
