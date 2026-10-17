using Balloonatic.Core;
using FluentAssertions;
using Xunit;

namespace Balloonatic.Tests
{
    /// <summary>
    /// The test-harness protocol is the same surface Playwright drives from JavaScript, so these
    /// tests are the fast half of the verification loop: if the contract holds here and the bridge
    /// is wired, the browser is only testing the bridge.
    /// </summary>
    public class TestHarnessTests
    {
        [Fact]
        public void Identical_scripts_produce_identical_hashes()
        {
            string Run()
            {
                TestHarness h = new TestHarness(0x1234);
                h.Handle("reset", "1234");
                h.Handle("set", "heat=1");
                h.Handle("step", "30");
                h.Handle("set", "heat=0");
                h.Handle("step", "30");
                return h.Handle("hash", "");
            }

            Run().Should().Be(Run());
        }

        [Fact]
        public void Reading_state_does_not_change_it()
        {
            TestHarness h = new TestHarness();
            string before = h.Handle("hash", "");

            h.Handle("hash", "").Should().Be(before);
            h.Handle("state", "").Should().Contain(before);
            h.Handle("step", "0").Should().StartWith("error:bad-arg");

            string after = h.Handle("step", "1");
            after.Should().NotBe(before).And.HaveLength(16);
            h.Handle("tick", "").Should().Be("1");
        }

        [Fact]
        public void Injected_input_changes_the_simulation()
        {
            TestHarness hot = new TestHarness();
            hot.Handle("reset", "99");
            hot.Handle("set", "heat=1");
            hot.Handle("step", "10");

            TestHarness cold = new TestHarness();
            cold.Handle("reset", "99");
            cold.Handle("step", "10");

            hot.Handle("hash", "").Should().NotBe(cold.Handle("hash", ""));
            hot.Handle("state", "").Should().Contain("\"heatTicks\":10");
        }

        [Fact]
        public void Reset_hands_time_control_to_the_caller()
        {
            TestHarness h = new TestHarness();
            h.Handle("resume", "");
            h.Handle("reset", "7").Should().StartWith("ok:");
            h.Paused.Should().BeTrue();

            // While paused the game loop contributes nothing, so scripted steps stay reproducible.
            h.AutoStep();
            h.Handle("tick", "").Should().Be("0");

            h.Handle("resume", "").Should().Be("ok:paused=0");
            h.AutoStep();
            h.Handle("tick", "").Should().Be("1");
        }

        [Fact]
        public void Ballast_is_queued_once_per_set()
        {
            TestHarness h = new TestHarness();
            h.Handle("set", "ballast=1");
            h.Handle("step", "1");
            h.Handle("step", "1");
            h.Handle("state", "").Should().Contain("\"ballastDropped\":1");
        }

        [Fact]
        public void Step_count_is_clamped_and_bounded()
        {
            TestHarness h = new TestHarness();
            h.Handle("step", "999999");
            h.Handle("tick", "").Should().Be(TestHarness.MaxStepsPerCall.ToString());
        }

        [Fact]
        public void Unknown_calls_and_arguments_fail_loudly()
        {
            TestHarness h = new TestHarness();
            h.Handle("nope", "").Should().Be("error:unknown-call:nope");
            h.Handle("set", "heat").Should().Be("error:bad-arg:set");
            h.Handle("set", "heat=maybe").Should().Be("error:bad-arg:set");
            h.Handle("set", "rudder=1").Should().Be("error:unknown-key:rudder");
            h.Handle("reset", "zzz").Should().Be("error:bad-arg:reset");
        }

        [Fact]
        public void The_bridge_exposes_no_horizontal_control()
        {
            // docs/CONCEPT.md critical design rule: the balloon has no left/right input, and the
            // protocol should not grow one by accident.
            TestHarness h = new TestHarness();
            h.Handle("set", "left=1").Should().StartWith("error:unknown-key");
            h.Handle("set", "right=1").Should().StartWith("error:unknown-key");
        }
    }
}
