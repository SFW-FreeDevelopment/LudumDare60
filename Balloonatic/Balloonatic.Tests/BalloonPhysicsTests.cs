using Balloonatic.Core;
using FluentAssertions;
using Xunit;

namespace Balloonatic.Tests
{
    /// <summary>
    /// The feel contract for the balloon, stated as behaviour: heat climbs, cold descends, ballast raises
    /// the ceiling, and momentum carries past the moment the burner closes. These are the properties the
    /// I5 feel gate will be judged against, so they must fail loudly if a tuning change breaks them.
    /// Everything here runs on the pure core at fixed step — no KNI, no browser.
    /// </summary>
    public class BalloonPhysicsTests
    {
        [Fact]
        public void Holding_heat_lifts_the_balloon_off_the_ground()
        {
            TestHarness h = new TestHarness();
            h.Handle("reset", "1234");
            h.Handle("set", "ballast=1");   // one bag away: a lighter craft breaks ground sooner
            h.Handle("set", "heat=1");
            h.Handle("step", "240");        // 4 s of burner

            float temperature = float.Parse(h.Handle("temperature", ""), System.Globalization.CultureInfo.InvariantCulture);
            float altitude = float.Parse(h.Handle("altitude", ""), System.Globalization.CultureInfo.InvariantCulture);

            temperature.Should().BeGreaterThan(0.8f);
            altitude.Should().BeGreaterThan(1f);
        }

        [Fact]
        public void Releasing_heat_brings_the_balloon_back_down()
        {
            TestHarness h = new TestHarness();
            h.Handle("reset", "1234");
            h.Handle("set", "heat=1");
            h.Handle("step", "600");        // 10 s: pegged temperature, climbing at terminal
            float peak = float.Parse(h.Handle("altitude", ""), System.Globalization.CultureInfo.InvariantCulture);

            h.Handle("set", "heat=0");
            h.Handle("step", "1200");       // 20 s of cooling
            float after = float.Parse(h.Handle("altitude", ""), System.Globalization.CultureInfo.InvariantCulture);
            float temperature = float.Parse(h.Handle("temperature", ""), System.Globalization.CultureInfo.InvariantCulture);

            after.Should().BeLessThan(peak);
            temperature.Should().BeApproximately(SimTuning.TempAmbient, 0.01f);
        }

        [Fact]
        public void Momentum_carries_up_after_the_burner_closes()
        {
            TestHarness h = new TestHarness();
            h.Handle("reset", "1234");
            h.Handle("set", "heat=1");
            h.Handle("step", "360");        // near terminal rise
            float before = float.Parse(h.Handle("altitude", ""), System.Globalization.CultureInfo.InvariantCulture);

            h.Handle("set", "heat=0");
            h.Handle("step", "30");
            float justAfter = float.Parse(h.Handle("altitude", ""), System.Globalization.CultureInfo.InvariantCulture);
            float velocity = float.Parse(h.Handle("velocity", ""), System.Globalization.CultureInfo.InvariantCulture);

            // The envelope is still hot and the air column still moving: it keeps rising for a moment.
            justAfter.Should().BeGreaterThan(before);
            velocity.Should().BeGreaterThan(0f);
        }

        [Fact]
        public void Terminal_rise_stops_accelerating_in_open_air()
        {
            LevelDef sky = OpenSky();
            SimState s = new SimState {Seed=1234, X=1000, Altitude=100};
            for(int i=0;i<600;i++)SimRules.Step(s,new SimInput{Heat=true},sky);
            float earlier=s.VelocityY;
            for(int i=0;i<120;i++)SimRules.Step(s,new SimInput{Heat=true},sky);
            float expected=(SimTuning.TempMax*SimTuning.LiftGain-SimTuning.Gravity-SimTuning.BagCapacity*SimTuning.BallastPerBag)/SimTuning.Drag;
            (s.VelocityY-earlier).Should().BeApproximately(0f,.5f);
            s.VelocityY.Should().BeApproximately(expected,1.5f);
        }

        static LevelDef OpenSky() => new LevelDef("open-sky", "Open sky", 4000f,
            new[]{new WindBand(0,4000,1,1)}, System.Array.Empty<GustZone>(), 10000, 120);

        [Fact]
        public void Less_ballast_gains_altitude_sooner_in_open_air()
        {
            float RiseWithBags(int bags)
            {
                SimState s=new SimState{Seed=1234,X=1000,Altitude=100,BallastHeld=bags};
                LevelDef sky=OpenSky();
                for(int i=0;i<600;i++)SimRules.Step(s,new SimInput{Heat=true},sky);
                return s.Altitude;
            }
            RiseWithBags(0).Should().BeGreaterThan(RiseWithBags(2));
        }

        [Fact]
        public void A_cold_balloon_launches_from_the_deck_and_comes_to_rest_when_the_wind_releases_it()
        {
            TestHarness h = new TestHarness();
            h.Handle("reset", "1234");

            // Runs start on the deck, not on the ground: the deck is the floor of a balloon's world.
            h.Handle("altitude", "").Should().Be(SimTuning.DeckTopAltitude.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            // And cold, the wind walks it off the deck: a launched-but-unburned balloon drifts down to
            // the dirt some distance home — the launch deck is not a parking space, it is a departure.
            h.Handle("step", "600");
            h.Handle("altitude", "").Should().Be(SimTuning.GroundAltitude.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
            h.Handle("velocity", "").Should().Be("0.00");
            float restX = float.Parse(h.Handle("x", ""), System.Globalization.CultureInfo.InvariantCulture);
            restX.Should().BeGreaterThan(SimTuning.DeckHalfWidth);   // came to rest past the deck
        }

        [Fact]
        public void Ballast_never_goes_negative()
        {
            TestHarness h = new TestHarness();
            h.Handle("reset", "1234");
            for (int i = 0; i < SimTuning.BagCapacity + 3; i++)
            {
                h.Handle("set", "ballast=1");
                h.Handle("step", "1");
            }

            h.Handle("ballast", "").Should().Be("0");
        }

        [Fact]
        public void Temperature_stays_in_bounds_under_any_pattern()
        {
            TestHarness h = new TestHarness();
            h.Handle("reset", "1234");

            for (int i = 0; i < 200; i++)
            {
                h.Handle("set", "heat=" + ((i * 37) % 5 < 3 ? 1 : 0));   // deterministic busy pattern
                h.Handle("step", "30");
                float temperature = float.Parse(h.Handle("temperature", ""), System.Globalization.CultureInfo.InvariantCulture);
                float altitude = float.Parse(h.Handle("altitude", ""), System.Globalization.CultureInfo.InvariantCulture);

                temperature.Should().BeInRange(SimTuning.TempAmbient, SimTuning.TempMax);
                altitude.Should().BeGreaterThanOrEqualTo(0f);
            }
        }

        [Fact]
        public void Every_state_field_moves_the_hash()
        {
            string Run(int steps, int drops)
            {
                TestHarness h = new TestHarness();
                h.Handle("reset", "1234");
                for (int i = 0; i < drops; i++)
                {
                    h.Handle("set", "ballast=1");
                    h.Handle("step", "1");
                }
                h.Handle("set", "heat=1");
                h.Handle("step", steps.ToString());
                return h.Handle("hash", "");
            }

            Run(120, 0).Should().NotBe(Run(121, 0));   // tick/temperature/altitude all move
            Run(120, 0).Should().NotBe(Run(120, 1));   // mass changes the future
            Run(120, 1).Should().Be(Run(120, 1));      // and it is reproducible
        }
    }
}
