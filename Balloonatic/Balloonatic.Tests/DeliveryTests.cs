using Balloonatic.Core;
using FluentAssertions;
using Xunit;

namespace Balloonatic.Tests
{
    /// <summary>
    /// The delivery loop and its landing rule (PLAN I5): decks are the only soft ground, speed
    /// decides soft from bounce, a soft delivery ends the run with its stars, and a finished run
    /// freezes while its world keeps ticking. Every authored level must also be *deliverable* by
    /// the autopilot — a level whose winds can reach the deck but never land on it is unbeatable,
    /// and that is exactly what this file refuses to ship.
    /// </summary>
    public class DeliveryTests
    {
        static float F(TestHarness h, string call) =>
            float.Parse(h.Handle(call, ""), System.Globalization.CultureInfo.InvariantCulture);

        static SimState At(float x, float altitude, float velocityY, LevelDef level)
        {
            SimState s = new SimState();
            s.X = x;
            s.Altitude = altitude;
            s.VelocityY = velocityY;
            s.LevelId = level.Id;
            s.Cargo = level.Package; // fixtures on final approach have already collected their parcel
            return s;
        }

        [Fact]
        public void A_soft_landing_on_the_delivery_deck_ends_the_run_won()
        {
            LevelDef level = LevelCatalog.Get(LevelCatalog.DefaultId);
            SimState s = At(level.CourseLength, SimTuning.DeckTopAltitude + 10f, -10f, level);

            for (int i = 0; i < 180 && !s.Delivered; i++)
                SimRules.Step(s, SimInput.None, level);

            s.Delivered.Should().BeTrue();
            s.Complete.Should().BeTrue();
            s.Altitude.Should().Be(SimTuning.DeckTopAltitude);
            s.VelocityY.Should().Be(0f);
            s.Stars.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public void A_fast_deck_contact_bounces_and_cost_a_star()
        {
            LevelDef level = LevelCatalog.Get(LevelCatalog.DefaultId);
            // Straight down at 200 px/s — no approach, no brake, just arrival. The deck throws it back
            // (the dive decays fast at this drag) — the first crossing arrives after a dozen steps.
            SimState s = At(level.CourseLength, SimTuning.DeckTopAltitude + 40f, -200f, level);

            for (int i = 0; i < 60 && s.Bounces == 0; i++)
                SimRules.Step(s, SimInput.None, level);

            s.Bounces.Should().Be(1);
            s.VelocityY.Should().BeGreaterThan(0f);          // the deck returned part of the speed
            s.Delivered.Should().BeFalse();                  // and the run goes on
            s.Complete.Should().BeFalse();

            // A bounced pilot who comes back — the way it was meant to be flown — still wins the deck,
            // but the bounce is remembered: the clean-landing star is gone for the run.
            s.X = level.CourseLength;
            s.VelocityX = 0f;
            s.Altitude = SimTuning.DeckTopAltitude + 10f;
            s.VelocityY = -10f;
            for (int i = 0; i < 120 && !s.Delivered; i++)
                SimRules.Step(s, SimInput.None, level);

            s.Delivered.Should().BeTrue();
            s.Stars.Should().Be(2);   // delivered + under par, minus the clean-landing star
        }

        [Fact]
        public void The_ground_is_always_soft_and_never_ends_the_run()
        {
            LevelDef level = LevelCatalog.Get(LevelCatalog.DefaultId);
            // Mid-course, off every deck: even a diving balloon just comes to rest on the dirt.
            SimState s = At(level.CourseLength / 2f, 200f, -300f, level);

            for (int i = 0; i < 600 && s.Altitude > SimTuning.GroundAltitude; i++)
                SimRules.Step(s, SimInput.None, level);

            s.Altitude.Should().Be(SimTuning.GroundAltitude);
            s.VelocityY.Should().Be(0f);
            s.Bounces.Should().Be(0);
            s.Complete.Should().BeFalse();
            s.Delivered.Should().BeFalse();
        }

        [Fact]
        public void A_soft_landing_on_the_launch_deck_is_a_pause_not_an_ending()
        {
            LevelDef level = LevelCatalog.Get(LevelCatalog.DefaultId);
            SimState s = At(0f, SimTuning.DeckTopAltitude + 10f, -10f, level);

            for (int i = 0; i < 60; i++)
                SimRules.Step(s, SimInput.None, level);

            s.Altitude.Should().Be(SimTuning.DeckTopAltitude);
            s.VelocityY.Should().Be(0f);
            s.Complete.Should().BeFalse();    // flying home to refuel is allowed; the run goes on
        }

        [Theory]
        [InlineData("nursery-skies")]
        [InlineData("winding-trail")]
        [InlineData("night-crossing")]
        public void Every_authored_level_can_actually_be_delivered(string levelId)
        {
            LevelDef level = LevelCatalog.Get(levelId);
            SimState s = new SimState { Seed = 0xBEEF, LevelId = level.Id };

            bool delivered = Autopilot.Deliver(s, level, 120f);

            delivered.Should().BeTrue($"{levelId} must be winnable: its winds must land a basket, not just reach it");
            s.X.Should().BeInRange(level.CourseLength - SimTuning.DeckHalfWidth, level.CourseLength + SimTuning.DeckHalfWidth);
            s.BallastHeld.Should().BeGreaterThan(0, "the delivery is flown with a landing brake in hand");
        }

        [Theory]
        [InlineData("nursery-skies")]
        [InlineData("winding-trail")]
        [InlineData("night-crossing")]
        public void The_ghost_pilot_lands_inside_par_so_the_speed_star_stays_earnable(string levelId)
        {
            // Par is a promise that a good human flight exists. The ghost pilot is the proof: if even
            // the planner cannot beat par (clean), the par is fiction and the second star is dead.
            LevelDef level = LevelCatalog.Get(levelId);
            SimState s = new SimState { Seed = 0xC0FFEE, LevelId = level.Id };

            Autopilot.Deliver(s, level, 120f).Should().BeTrue();

            s.Stars.Should().Be(3, $"{levelId}: a flown-by-the-book delivery must earn all three stars");
            s.Bounces.Should().Be(0);
            s.Tick.Should().BeLessThan(level.ParSeconds * Sim.StepHz);
        }

        [Theory]
        [InlineData("nursery-skies")]
        [InlineData("winding-trail")]
        [InlineData("night-crossing")]
        public void Delivery_is_reproducible_from_one_seed(string levelId)
        {
            LevelDef level = LevelCatalog.Get(levelId);
            SimState a = new SimState { Seed = 0xC0FFEE, LevelId = level.Id };
            SimState b = new SimState { Seed = 0xC0FFEE, LevelId = level.Id };

            Autopilot.Deliver(a, level, 120f);
            Autopilot.Deliver(b, level, 120f);

            SimRules.HashHex(a).Should().Be(SimRules.HashHex(b));
        }

        [Fact]
        public void A_finished_run_ignores_the_burner_but_keeps_its_clock()
        {
            TestHarness h = new TestHarness();
            h.Handle("reset", "beef");
            h.Handle("deliver", "120").Should().Be("ok:delivered");

            float temperature = h.State.Temperature;
            int tick = h.State.Tick;
            h.Handle("set", "heat=1");
            h.Handle("step", "600");

            h.Handle("temperature", "").Should().Be(temperature.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture));
            h.Handle("tick", "").Should().Be((tick + 600).ToString(System.Globalization.CultureInfo.InvariantCulture));
            h.Handle("delivered", "").Should().Be("1");
            // The freeze must not freeze the hash: tick and seed still move it.
            h.Handle("step", "60");
        }

        [Fact]
        public void Restart_replays_the_same_sky_from_the_launch_deck()
        {
            TestHarness h = new TestHarness();
            string freshHash = h.Handle("reset", "1234").Substring(3);

            h.Handle("resume", "");
            h.Handle("set", "heat=1");
            h.Handle("step", "300");
            F(h, "altitude").Should().BeGreaterThan(SimTuning.DeckTopAltitude);

            h.Handle("restart", "").Should().Be("ok:" + freshHash);   // same seed ⇒ literally the same opening
            h.Paused.Should().BeFalse();                          // and play continues unpaused
            h.Handle("seed", "").Should().Be("0000000000001234");
            F(h, "altitude").Should().Be(SimTuning.DeckTopAltitude);
        }

        [Fact]
        public void The_bridge_reports_the_landing_fields()
        {
            TestHarness h = new TestHarness();
            h.Handle("reset", "beef");
            h.Handle("complete", "").Should().Be("0");
            h.Handle("stars", "").Should().Be("0");
            h.Handle("bounces", "").Should().Be("0");

            h.Handle("deliver", "120").Should().Be("ok:delivered");
            h.Handle("delivered", "").Should().Be("1");
            h.Handle("complete", "").Should().Be("1");
            int.Parse(h.Handle("stars", "")).Should().BeGreaterThanOrEqualTo(1);
            h.Handle("state", "").Should().Contain("\"stars\":").And.Contain("\"delivered\":true");
        }

        [Fact]
        public void Completion_freezes_the_results_clock_while_the_world_keeps_ticking()
        {
            TestHarness h = new TestHarness();
            h.Handle("reset", "beef");
            h.Handle("deliver", "120").Should().Be("ok:delivered");

            int frozen = h.State.CompletedAtTick;
            frozen.Should().BeGreaterThan(0);                 // the delivery stamped the results clock...

            h.Handle("step", "600");
            F(h, "tick").Should().BeGreaterThan(frozen);      // ...the world's clock honestly keeps going...
            h.State.CompletedAtTick.Should().Be(frozen);      // ...and the delivered time is a finished number
            h.Handle("state", "").Should().Contain("\"completedAtTick\":" + frozen);
        }

        [Fact]
        public void Advance_walks_the_courses_in_order_and_loops_back()
        {
            TestHarness h = new TestHarness();
            h.Handle("reset", "beef");
            h.Handle("advance", "").Should().StartWith("ok:");

            h.State.LevelId.Should().Be("winding-trail");         // R after a delivery flies the next course
            h.State.Delivered.Should().BeFalse();                 // as a fresh run: nothing won yet...
            h.State.BallastHeld.Should().Be(SimTuning.BagCapacity); // ...and the brake is back in hand
            F(h, "altitude").Should().Be(SimTuning.DeckTopAltitude); // on the launch deck again
            h.Handle("seed", "").Should().NotBe("000000000000beef"); // a different sky, not a replay

            h.Handle("advance", "");
            h.State.LevelId.Should().Be("night-crossing");
            h.Handle("advance", "");
            h.State.LevelId.Should().Be("nursery-skies");         // the last course sends you home to fly again
        }

        [Fact]
        public void A_dropped_bag_is_recorded_where_it_was_released_and_moves_the_hash()
        {
            // The renderer draws the falling bag from these records, so the records are the contract:
            // one per real release, at the exact world position, and covered by the state hash.
            TestHarness a = new TestHarness();
            TestHarness b = new TestHarness();
            a.Handle("reset", "b091");   // hex, like every seed
            b.Handle("reset", "b091");

            a.Handle("set", "ballast=1");
            a.Handle("step", "1");
            a.State.DropsSeen.Should().Be(1);
            a.State.DropTick[0].Should().Be(a.State.Tick);                   // recorded the step it left
            a.State.DropAltitude[0].Should().Be(SimTuning.DeckTopAltitude);  // released from the deck
            a.State.BallastHeld.Should().Be(SimTuning.BagCapacity - 1);
            a.Handle("state", "").Should().Contain("\"drops\":1");
            b.Handle("step", "1");                                           // same tick, same sky —
            a.Handle("hash", "").Should().NotBe(b.Handle("hash", ""));       // only the dropped bag differs

            a.Handle("set", "ballast=1");
            a.Handle("step", "30");
            a.State.DropsSeen.Should().Be(2);                                // second bag, second record
            a.Handle("set", "ballast=1");
            a.Handle("step", "30");
            a.State.DropsSeen.Should().Be(2);                                // nothing held: nothing to drop,
            a.State.BallastHeld.Should().Be(0);                              // and nothing remembered
        }

        [Fact]
        public void The_same_playthrough_seed_grows_the_same_sky_on_every_head()
        {
            // The advance chain must be pure arithmetic: desktop and WASM grow identical skies from
            // identical seeds (object hashcodes would break exactly this across processes).
            TestHarness a = new TestHarness(0xABCD);
            TestHarness b = new TestHarness(0xABCD);
            for (int lap = 0; lap < 4; lap++)
            {
                a.Handle("advance", "");
                b.Handle("advance", "");
                a.Handle("level", "").Should().Be(b.Handle("level", ""));
                a.Handle("seed", "").Should().Be(b.Handle("seed", ""));
            }
        }
    }
}
