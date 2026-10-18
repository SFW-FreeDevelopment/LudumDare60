using Balloonatic.Core;
using FluentAssertions;
using Xunit;
namespace Balloonatic.Tests;
public class CargoTests
{
    [Fact]
    public void Launch_pickup_is_carried_until_delivery_and_consumed_once()
    {
        SimState s=new(){Seed=1234};
        s.CargoStatus.Should().Be("waiting");
        SimRules.Step(s,SimInput.None);
        s.Cargo.Should().Be(LevelCatalog.Get(s.LevelId).Package);
        s.CargoStatus.Should().Be("carried");
        int pickup=s.PickupTick;
        Autopilot.Deliver(s,LevelCatalog.Get(s.LevelId),120).Should().BeTrue();
        s.CargoStatus.Should().Be("delivered");s.Cargo.Should().Be(CargoKind.None);
        SimRules.Step(s,new SimInput{Heat=true});s.PickupTick.Should().Be(pickup);
    }
    [Theory]
    [InlineData(CargoKind.None)][InlineData(CargoKind.Letter)]
    public void Empty_or_wrong_cargo_cannot_complete_a_delivery(CargoKind cargo)
    {
        SimState s=new(){Seed=1234,X=600,Altitude=43,VelocityY=-10,Cargo=cargo};
        for(int i=0;i<20;i++)SimRules.Step(s,SimInput.None);
        s.Delivered.Should().BeFalse();s.Complete.Should().BeFalse();
    }
}
