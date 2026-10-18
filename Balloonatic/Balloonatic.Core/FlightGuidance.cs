using System;
namespace Balloonatic.Core;

/// <summary>The instruments and test bridge share the same interpretation of a flight.</summary>
public static class FlightGuidance
{
    public static string Expression(SimState state) => state.Delivered ? "happy" :
        state.Hearts < SimTuning.HeartsStart && state.Tick - state.HurtTick < SimTuning.InvulnTicks ? "worried" :
        state.PickupTick >= 0 && state.Tick - state.PickupTick < 120 ? "happy" : "idle";
    public static int BandIndex(SimState state, LevelDef level)
    {
        for(int i=0;i<level.Bands.Length;i++)
            if(level.Bands[i].Contains(state.Altitude))return i;
        return Math.Max(0,level.Bands.Length-1);
    }

    public static string HeatTrend(SimState state) => state.Heating ? "heating" :
        state.Temperature > SimTuning.TempAmbient + .01f ? "cooling" : "cool";

    public static string Approach(SimState state, LevelDef level)
    {
        if(state.Delivered)return "delivered";
        if(Math.Abs(state.X-level.CourseLength)>160f || state.Altitude>SimTuning.DeckTopAltitude+140f)return "travel";
        if(state.VelocityY < -SimTuning.SoftLandingVy)return "fast";
        return state.VelocityY > 3f ? "rising" : "gentle";
    }
}
