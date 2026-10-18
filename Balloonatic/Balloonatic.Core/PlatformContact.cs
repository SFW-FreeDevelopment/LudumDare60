using System;
namespace Balloonatic.Core;

/// <summary>One-way timber surfaces support the basket footprint; wind still moves a landed craft.</summary>
public static class PlatformContact
{
    static bool Overlaps(float x,float centre,float halfWidth)=>
        Math.Abs(x-centre)<=halfWidth+SimTuning.BasketFootHalfWidth;

    public static string Support(SimState state,LevelDef level)
    {
        if(state.VelocityY!=0f)return "air";
        bool At(float height)=>Math.Abs(state.Altitude-height)<.001f;
        if(At(SimTuning.DeckTopAltitude)) {
            if(Overlaps(state.X,level.CourseLength,SimTuning.DeckHalfWidth))return "delivery";
            if(Overlaps(state.X,0,SimTuning.DeckHalfWidth))return "launch";
        }
        foreach(RestDeck deck in level.Platforms)
            if(At(deck.Altitude)&&Overlaps(state.X,deck.X,deck.HalfWidth))return "perch";
        return At(SimTuning.GroundAltitude)?"ground":"air";
    }

    public static void Resolve(SimState state,LevelDef level,float previousX,float previousAltitude)
    {
        if(state.VelocityY>=0f)return;
        float surface=float.NegativeInfinity;
        bool recipient=false;
        void Check(float centre,float height,float halfWidth,bool delivery)
        {
            if(height<=surface||previousAltitude<height||state.Altitude>height)return;
            float fall=previousAltitude-state.Altitude;
            float time=fall>0f?(previousAltitude-height)/fall:0f;
            float contactX=previousX+(state.X-previousX)*time;
            if(!Overlaps(contactX,centre,halfWidth))return;
            surface=height;recipient=delivery;
        }
        Check(0,SimTuning.DeckTopAltitude,SimTuning.DeckHalfWidth,false);
        Check(level.CourseLength,SimTuning.DeckTopAltitude,SimTuning.DeckHalfWidth,true);
        foreach(RestDeck deck in level.Platforms)Check(deck.X,deck.Altitude,deck.HalfWidth,false);
        if(float.IsNegativeInfinity(surface))return;
        state.Altitude=surface;
        if(-state.VelocityY>SimTuning.SoftLandingVy) {
            state.VelocityY=-state.VelocityY*SimTuning.BounceRestitution;
            state.Bounces++;state.BounceTick=state.Tick;return;
        }
        state.VelocityY=0f;
        if(!recipient||state.Cargo!=level.Package||Math.Abs(state.X-level.CourseLength)>SimTuning.RecipientHalfWidth)return;
        state.Cargo=CargoKind.None;state.Delivered=true;state.Complete=true;state.CompletedAtTick=state.Tick;
        state.Stars=1+(state.Tick<=level.ParSeconds*Sim.StepHz?1:0)+(state.Bounces==0?1:0);
    }
}
