using Balloonatic.Core;
using FluentAssertions;
using Xunit;
namespace Balloonatic.Tests;
public class GuidanceTests
{
    [Theory]
    [InlineData(-80,"fast")][InlineData(-12,"gentle")][InlineData(20,"rising")]
    public void Approach_distinguishes_an_impact_from_a_gentle_descent(float velocity,string expected)
    {
        SimState s=new(){X=560,Altitude=90,VelocityY=velocity};
        FlightGuidance.Approach(s,LevelCatalog.Get(s.LevelId)).Should().Be(expected);
    }
}
