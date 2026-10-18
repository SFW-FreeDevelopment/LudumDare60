using Balloonatic.Core;
using FluentAssertions;
using Xunit;
namespace Balloonatic.Tests;
public class CeilingTests
{
    [Theory]
    [InlineData("nursery-skies")][InlineData("winding-trail")][InlineData("night-crossing")]
    public void The_top_breeze_stays_in_view_and_cooling_releases_it(string id)
    {
        LevelDef level=LevelCatalog.Get(id);
        SimState s=new(){Seed=1234,LevelId=id,X=100,Altitude=level.WorldHeight-1,VelocityY=40,Temperature=.9f,BallastHeld=0};
        for(int i=0;i<30;i++)SimRules.Step(s,new SimInput{Heat=true},level);
        s.Altitude.Should().Be(level.WorldHeight);s.VelocityY.Should().Be(0);
        for(int i=0;i<240;i++)SimRules.Step(s,SimInput.None,level);
        s.Altitude.Should().BeLessThan(level.WorldHeight);s.Recoveries.Should().Be(0);
    }
}
