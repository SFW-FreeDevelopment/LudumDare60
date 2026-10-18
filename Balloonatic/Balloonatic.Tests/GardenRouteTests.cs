using System.Collections.Generic;
using Balloonatic.Core;
using FluentAssertions;
using Xunit;
namespace Balloonatic.Tests;
public class GardenRouteTests
{
    [Theory][InlineData(1UL)][InlineData(1234UL)][InlineData(0x6060UL)]
    public void The_letter_route_meets_the_gust_bank_and_lands_with_normal_collisions(ulong seed)
    {
        LevelDef level=LevelCatalog.Get("night-crossing");level.Package.Should().Be(CargoKind.Letter);
        SimState flown=new(){Seed=seed,LevelId=level.Id},replay=flown.Clone();
        List<SimInput> inputs=new();int gustSteps=0;
        Autopilot.Deliver(flown,level,120,hazards:true,record:input=>{
            inputs.Add(input);if(level.StormWindAt(flown.X,flown.Altitude,flown.Tick)!=0)gustSteps++;
        }).Should().BeTrue();
        gustSteps.Should().BeGreaterThan(0);flown.Hearts.Should().BeGreaterThan(0);
        foreach(SimInput input in inputs)SimRules.Step(replay,input,level);
        replay.Delivered.Should().BeTrue();replay.Bounces.Should().Be(0);
        SimRules.HashHex(replay).Should().Be(SimRules.HashHex(flown));
    }
}
