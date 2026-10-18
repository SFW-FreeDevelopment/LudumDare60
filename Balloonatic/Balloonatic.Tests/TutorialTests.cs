using System.Collections.Generic;
using Balloonatic.Core;
using FluentAssertions;
using Xunit;
namespace Balloonatic.Tests;
public class TutorialTests
{
    [Theory]
    [InlineData("nursery-skies")][InlineData("winding-trail")][InlineData("night-crossing")]
    public void A_recorded_legal_flight_replays_with_normal_collisions(string id)
    {
        LevelDef level=LevelCatalog.Get(id);
        SimState flown=new(){Seed=0x6060,LevelId=id};
        SimState replay=flown.Clone();
        List<SimInput> inputs=new();
        Autopilot.Deliver(flown,level,120,hazards:true,record:inputs.Add).Should().BeTrue(id);
        foreach(SimInput input in inputs)SimRules.Step(replay,input,level);
        replay.Delivered.Should().BeTrue();replay.Hearts.Should().BeGreaterThan(0);
        SimRules.HashHex(replay).Should().Be(SimRules.HashHex(flown));
        if(id=="nursery-skies")replay.Hearts.Should().Be(3,"the teaching route should be safe");
    }

    [Fact]
    public void The_breeze_perch_supports_a_soft_landing_without_completing_delivery()
    {
        SimState s=new(){Seed=1234,X=220,Altitude=124,VelocityY=-10,Cargo=CargoKind.Teddy};
        for(int i=0;i<30;i++)SimRules.Step(s,SimInput.None);
        s.Altitude.Should().Be(120);s.Complete.Should().BeFalse();s.Cargo.Should().Be(CargoKind.Teddy);
    }
}
