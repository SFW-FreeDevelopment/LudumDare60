using Balloonatic.Core;
using FluentAssertions;
using Xunit;
namespace Balloonatic.Tests;

public class PerchTests
{
    [Theory][InlineData("nursery-skies")][InlineData("winding-trail")][InlineData("night-crossing")]
    public void Each_perch_parks_the_basket_until_the_burner_is_held(string id)
    {
        LevelDef level=LevelCatalog.Get(id);
        foreach(RestDeck deck in level.Platforms) {
            SimState state=new(){LevelId=id,X=deck.X,Altitude=deck.Altitude+.01f,VelocityY=-5,
                VelocityX=level.WindAt(deck.Altitude),Cargo=level.Package};
            SimRules.Step(state,SimInput.None,level,hazards:false);
            float parkedX=state.X;
            for(int i=0;i<600;i++)SimRules.Step(state,SimInput.None,level,hazards:false);
            state.X.Should().Be(parkedX);state.Altitude.Should().Be(deck.Altitude);
            state.VelocityX.Should().Be(0);state.VelocityY.Should().Be(0);state.Cargo.Should().Be(level.Package);
            state.Complete.Should().BeFalse();FlightGuidance.ControlHint(state,level).Should().Be("PERCHED: SPACE");
            for(int i=0;i<120;i++)SimRules.Step(state,new SimInput{Heat=true},level,hazards:false);
            state.Altitude.Should().BeGreaterThan(deck.Altitude+8);PlatformContact.Support(state,level).Should().Be("air");
        }
    }
    [Fact]
    public void Nearby_guidance_explains_approaching_from_above_and_releasing_to_rest()
    {
        LevelDef level=LevelCatalog.Get(LevelCatalog.DefaultId);
        SimState state=new(){X=220,Altitude=100};
        FlightGuidance.ControlHint(state,level).Should().Be("RISE ABOVE DECK");
        state.Altitude=150;FlightGuidance.ControlHint(state,level).Should().Be("LAND TO REST");
        state.Heating=true;FlightGuidance.ControlHint(state,level).Should().Be("LET GO TO LAND");
    }
}
