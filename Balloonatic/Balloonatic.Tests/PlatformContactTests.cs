using System;
using System.Collections.Generic;
using Balloonatic.Core;
using FluentAssertions;
using Xunit;
namespace Balloonatic.Tests;

public class PlatformContactTests
{
    public static IEnumerable<object[]> Edges()
    {
        foreach(LevelDef level in LevelCatalog.Levels) {
            var decks=new List<(string Kind,float X,float Y,float Half)> {
                ("launch",0,38,56),("delivery",level.CourseLength,38,56)};
            foreach(RestDeck deck in level.Platforms)decks.Add(("perch",deck.X,deck.Altitude,deck.HalfWidth));
            foreach(var deck in decks)foreach(int side in new[]{-1,1})
                yield return new object[]{level.Id,deck.Kind,deck.X+side*(deck.Half+8),deck.Y};
        }
    }
    public static IEnumerable<object[]> LoadedEdges()
    {
        foreach(object[] edge in Edges())foreach(int bags in new[]{0,1,2})
            yield return new object[]{edge[0],edge[1],edge[2],edge[3],bags};
    }
    [Theory][MemberData(nameof(LoadedEdges))]
    public void Every_deck_supports_a_partial_basket_touchdown_with_every_loadout(string id,string kind,float x,float height,int bags)
    {
        LevelDef level=LevelCatalog.Get(id);
        SimState state=new(){LevelId=id,X=x,Altitude=height+.01f,VelocityY=-5,BallastHeld=bags,Cargo=level.Package};
        SimRules.Step(state,SimInput.None,level,hazards:false);
        state.Altitude.Should().Be(height);state.VelocityY.Should().Be(0);state.Bounces.Should().Be(0);
        PlatformContact.Support(state,level).Should().Be(kind);state.Complete.Should().BeFalse();
    }
    [Theory][MemberData(nameof(Edges))]
    public void Hard_edge_contacts_still_bounce(string id,string kind,float x,float height)
    {
        SimState state=new(){LevelId=id,X=x,Altitude=height+1,VelocityY=-200};
        SimRules.Step(state,SimInput.None,LevelCatalog.Get(id),hazards:false);
        state.Bounces.Should().Be(1);state.VelocityY.Should().BePositive();state.Complete.Should().BeFalse();
        PlatformContact.Support(state,LevelCatalog.Get(id)).Should().Be("air",kind);
    }
    [Theory][InlineData("nursery-skies",0)][InlineData("nursery-skies",1)][InlineData("nursery-skies",2)]
    [InlineData("winding-trail",0)][InlineData("winding-trail",1)][InlineData("winding-trail",2)]
    [InlineData("night-crossing",0)][InlineData("night-crossing",1)][InlineData("night-crossing",2)]
    public void A_landed_basket_slides_into_the_recipient_in_both_wind_directions(string id,int bags)
    {
        LevelDef level=LevelCatalog.Get(id);float wind=level.WindAt(38);
        SimState state=new(){LevelId=id,X=level.CourseLength-Math.Sign(wind)*64,Altitude=38.01f,VelocityY=-5,
            VelocityX=wind,BallastHeld=bags,Cargo=level.Package};
        SimRules.Step(state,SimInput.None,level,hazards:false);
        state.Delivered.Should().BeFalse();PlatformContact.Support(state,level).Should().Be("delivery");
        state.VelocityX.Should().NotBe(0,"deck contact preserves drift");
        for(int tick=0;tick<360&&!state.Delivered;tick++)SimRules.Step(state,SimInput.None,level,hazards:false);
        state.Delivered.Should().BeTrue();state.Cargo.Should().Be(CargoKind.None);state.Bounces.Should().Be(0);
        state.Altitude.Should().Be(38);state.CompletedAtTick.Should().BeGreaterThan(60);
    }
    [Fact]
    public void A_resting_perch_uses_the_wind_at_its_surface_not_the_layer_under_the_planks()
    {
        LevelDef level=LevelCatalog.Get(LevelCatalog.DefaultId);
        SimState state=new(){X=220,Altitude=120,VelocityX=level.WindAt(120)};
        SimRules.Step(state,SimInput.None,level,hazards:false);
        state.VelocityX.Should().Be(level.WindAt(120));state.Altitude.Should().Be(120);
    }
    [Fact]
    public void The_ground_does_not_sample_the_top_band_when_gravity_pushes_below_zero()
    {
        LevelDef level=LevelCatalog.Get(LevelCatalog.DefaultId);
        SimState state=new(){X=350,Altitude=0,VelocityX=level.WindAt(0)};
        SimRules.Step(state,SimInput.None,level,hazards:false);
        state.VelocityX.Should().Be(level.WindAt(0));state.Altitude.Should().Be(0);
    }
    [Fact]
    public void Contact_uses_horizontal_position_at_the_surface_crossing()
    {
        SimState state=new(){X=600-56-SimTuning.BasketFootHalfWidth-.02f,Altitude=38.01f,VelocityY=-5,VelocityX=30};
        SimRules.Step(state,SimInput.None,LevelCatalog.Get(state.LevelId),hazards:false);
        state.Altitude.Should().Be(38);state.VelocityY.Should().Be(0);
    }
    [Theory][InlineData("nursery-skies")][InlineData("winding-trail")][InlineData("night-crossing")]
    public void Perches_allow_takeoff_falling_off_and_do_not_catch_from_below(string id)
    {
        LevelDef level=LevelCatalog.Get(id);
        foreach(RestDeck deck in level.Platforms) {
            SimState rising=new(){LevelId=id,X=deck.X,Altitude=deck.Altitude,
                Temperature=(SimTuning.Gravity+16)/SimTuning.LiftGain+.05f};
            SimRules.Step(rising,new SimInput{Heat=true},level,hazards:false);
            rising.Altitude.Should().BeGreaterThan(deck.Altitude);PlatformContact.Support(rising,level).Should().Be("air");
            float wind=level.WindAt(deck.Altitude);
            SimState leaving=new(){LevelId=id,X=deck.X+Math.Sign(wind)*(deck.HalfWidth+SimTuning.BasketFootHalfWidth-.05f),
                Altitude=deck.Altitude,VelocityX=wind};
            for(int i=0;i<10;i++)SimRules.Step(leaving,SimInput.None,level,hazards:false);
            leaving.Altitude.Should().BeLessThan(deck.Altitude);leaving.Delivered.Should().BeFalse();
            SimState below=new(){LevelId=id,X=deck.X,Altitude=deck.Altitude-1};
            SimRules.Step(below,SimInput.None,level,hazards:false);
            below.Altitude.Should().BeLessThan(deck.Altitude-1);PlatformContact.Support(below,level).Should().Be("air");
        }
    }
}
