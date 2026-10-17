using Balloonatic.Core;
using FluentAssertions;
using Xunit;

namespace Balloonatic.Tests
{
    /// <summary>
    /// I7 settings: the store, the mirror the menus are drawn from, and the one setting the physics
    /// actually feels. AppSettings is session state (like a real options menu), so every test here
    /// restores the defaults it started from and cannot leak into its neighbours.
    /// </summary>
    public class SettingsTests
    {
        [Fact]
        public void TrySetRoundTripsAndRejectsNonsense()
        {
            try
            {
                AppSettings.TrySet("music", "5").Should().BeNull();
                AppSettings.TrySet("assist", "on").Should().BeNull();
                AppSettings.Describe().Should().Contain("music=5").And.Contain("assist=1");

                AppSettings.TrySet("music", "11").Should().Be("error:bad-arg:music");
                AppSettings.TrySet("assist", "maybe").Should().Be("error:bad-arg:assist");
                AppSettings.TrySet("gravitas", "1").Should().Be("error:unknown-key:gravitas");
                AppSettings.MusicVolume.Should().Be(5);   // a rejected write changed nothing
            }
            finally
            {
                AppSettings.RestoreDefaults();
            }
        }

        [Fact]
        public void WindAssistIgnoresGustsButKeepsTheBands()
        {
            // Toy Isles carries a -14 gust inside 240..300 on top of a -28 band wind. With assist
            // the balloon must settle on the band alone: gentler air, and altitude still steers.
            try
            {
                AppSettings.WindAssist = true;
                SimState calm = FlyAtAltitude(270f, assist: true);
                SimState raw = FlyAtAltitude(270f, assist: false);

                calm.WindAssist.Should().BeTrue();
                calm.VelocityX.Should().BeApproximately(-LevelDef.WindSpeed(2), 1f);
                raw.VelocityX.Should().BeLessThan(-LevelDef.WindSpeed(2) - 5f);   // the gust is biting
                calm.Altitude.Should().Be(raw.Altitude);                           // vertical play is untouched
            }
            finally
            {
                AppSettings.RestoreDefaults();
            }
        }

        static SimState FlyAtAltitude(float altitude, bool assist)
        {
            SimState state = new SimState { Seed = 0x1717, LevelId = "winding-trail" };
            state.WindAssist = assist;
            state.Altitude = altitude;
            for (int i = 0; i < 180; i++)                 // three seconds is two settle time-constants
            {
                SimRules.Step(state, SimInput.None);
                // Hold the altitude: this test is about horizontal truth, and an ambient-temperature
                // balloon would otherwise sink out of the gust zone before the wind finishes settling.
                state.Altitude = altitude;
                state.VelocityY = 0f;
            }
            return state;
        }

        [Fact]
        public void HarnessExposesSettingsAndTheLiveStateFeelsAssist()
        {
            try
            {
                TestHarness harness = new TestHarness();
                harness.Handle("settings", "").Should().Be(AppSettings.Describe());

                harness.Handle("settings", "music=3").Should().StartWith("ok:").And.Contain("music=3");
                harness.Handle("state", "").Should().Contain("\"settings\":\"music=3");

                harness.Handle("settings", "assist=1").Should().Contain("assist=1");
                harness.State.WindAssist.Should().BeTrue();       // the flight in the air feels it now
                harness.Handle("reset", "1234");
                harness.State.WindAssist.Should().BeTrue();       // and a new run inherits it
            }
            finally
            {
                AppSettings.RestoreDefaults();
            }
        }
    }
}
