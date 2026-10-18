using System;
using System.Collections.Generic;
using Balloonatic.Core;
using FluentAssertions;
using Xunit;
namespace Balloonatic.Tests;
public class ToyRouteTests
{
    [Theory][InlineData(1UL)][InlineData(1234UL)][InlineData(0x6060UL)]
    public void The_teddy_route_uses_carrying_air_then_returns_to_a_soft_landing(ulong seed)
    {
        LevelDef level=LevelCatalog.Get("winding-trail");
        level.Bands[0].Direction.Should().Be(-1);level.Package.Should().Be(CargoKind.Teddy);
        SimState flown=new(){Seed=seed,LevelId=level.Id},replay=flown.Clone();
        List<SimInput> inputs=new();float highest=0;
        Autopilot.Deliver(flown,level,120,hazards:true,record:input=>{inputs.Add(input);highest=Math.Max(highest,flown.Altitude);}).Should().BeTrue();
        highest.Should().BeGreaterThan(110);flown.Hearts.Should().BeGreaterThan(0);
        foreach(SimInput input in inputs)SimRules.Step(replay,input,level);
        replay.Delivered.Should().BeTrue();replay.Bounces.Should().Be(0);
        SimRules.HashHex(replay).Should().Be(SimRules.HashHex(flown));
    }
}
