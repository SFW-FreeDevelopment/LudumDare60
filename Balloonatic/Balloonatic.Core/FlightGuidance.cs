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

    public static string ControlHint(SimState state, LevelDef level) {
        if(PlatformContact.Support(state,level)=="perch")return state.Heating?"HOLD TO LIFT":"PERCHED: SPACE";
        if(!state.Complete&&PlatformContact.Support(state,level)=="delivery")return "DECK: DRIFT IN";
        foreach(StormZone zone in level.Storms) {
            if(Math.Abs(state.Altitude-(zone.FromAltitude+zone.ToAltitude)/2)<70 && state.X>=zone.FromX-120 && state.X<=zone.ToX)
                return state.WindAssist?"ASSIST: CALM AIR":state.X<zone.FromX?"GUST AHEAD":"GUST: WATCH WIND";
        }
        foreach(RestDeck deck in level.Platforms)
            if(Math.Abs(state.X-deck.X)<deck.HalfWidth+60 && state.Altitude>deck.Altitude-80 && state.Altitude<deck.Altitude+100)
                return state.Altitude<deck.Altitude?"RISE ABOVE DECK":state.Heating?"LET GO TO LAND":"LAND TO REST";
        return state.Altitude >= level.WorldHeight-.5f ? "LET GO TO DESCEND" :
            level.Bands[0].Direction < 0 && BandIndex(state,level)==0 && state.X<level.CourseLength-160 ? "RISE FOR > WIND" :
            state.Heating ? "SPACE: HEATING" : HeatTrend(state)=="cooling" ? "COOLING..." : "SPACE: RISE";
    }

    public static string Approach(SimState state, LevelDef level)
    {
        if(state.Delivered)return "delivered";
        if(PlatformContact.Support(state,level)=="perch")return "perched";
        if(PlatformContact.Support(state,level)=="delivery")return "landed";
        if(Math.Abs(state.X-level.CourseLength)>160f || state.Altitude>SimTuning.DeckTopAltitude+140f)return "travel";
        if(state.VelocityY < -SimTuning.SoftLandingVy)return "fast";
        return state.VelocityY > 3f ? "rising" : "gentle";
    }
}
