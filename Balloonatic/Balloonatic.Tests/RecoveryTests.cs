using Balloonatic.Core;
using FluentAssertions;
using Xunit;

namespace Balloonatic.Tests;
public class RecoveryTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void Every_loadout_can_cool_descend_and_land_without_another_bag(int bags)
    {
        LevelDef calm = new("calm", "Calm", 400, new[]{new WindBand(0,400,1,1)}, System.Array.Empty<GustZone>(),600,60);
        SimState state = new(){Seed=1234, X=600, Altitude=160, BallastHeld=bags, VelocityY=0};
        for(int i=0;i<1200 && !state.Delivered;i++)
        {
            state.X=600; state.VelocityX=0; // isolates descent from the wind puzzle
            SimRules.Step(state, SimInput.None, calm);
        }
        state.Delivered.Should().BeTrue();
        state.Bounces.Should().Be(0);
        state.BallastHeld.Should().Be(bags);
    }

    [Theory]
    [InlineData(-250)] [InlineData(850)]
    public void A_missed_course_returns_safely_without_spending_health_or_refilling_bags(float x)
    {
        SimState state = new(){Seed=1234,X=x,Altitude=140,BallastHeld=0};
        SimRules.Step(state,SimInput.None);
        state.Recoveries.Should().Be(1);
        state.X.Should().BeInRange(0,600);
        state.Hearts.Should().Be(3);
        state.BallastHeld.Should().Be(0);
        SimRules.HashHex(state.Clone()).Should().Be(SimRules.HashHex(state));
    }
}
