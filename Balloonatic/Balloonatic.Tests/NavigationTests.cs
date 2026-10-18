using Balloonatic.Core;
using FluentAssertions;
using Xunit;
namespace Balloonatic.Tests;
public class NavigationTests
{
    [Fact]
    public void Selecting_a_course_after_delivery_clears_finished_flight_and_pending_input()
    {
        TestHarness h=new();h.Handle("deliver","120").Should().Be("ok:delivered");
        int tick=h.State.Tick;ulong seed=h.State.Seed;
        h.Handle("set","heat=1");h.Handle("set","ballast=1");
        h.Handle("level","winding-trail").Should().Be("ok:level=winding-trail");
        h.State.Tick.Should().Be(tick);h.State.Seed.Should().Be(seed);
        h.State.Complete.Should().BeFalse();h.State.Delivered.Should().BeFalse();h.State.Cargo.Should().Be(CargoKind.None);
        h.State.PickupTick.Should().Be(-1);h.State.Hearts.Should().Be(3);h.State.DropsSeen.Should().Be(0);
        h.Handle("step","1");h.State.Heating.Should().BeFalse();h.State.BallastHeld.Should().Be(2);
    }
}
