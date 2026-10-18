using System;
using Balloonatic.Core;
namespace Balloonatic.Rendering;

/// <summary>One atlas quad contains a board, its text and post. They travel on the world grid.</summary>
static class WorldSigns
{
    public readonly record struct Placement(string Asset,float X,float Y,float ParentX,float ParentY)
    {
        public string Json(string text)=>FormattableString.Invariant($"{{\"text\":\"{text}\",\"asset\":\"{Asset}\",\"ready\":{(Sprites.Has(Asset)?"true":"false")},\"x\":{X},\"y\":{Y},\"parentX\":{ParentX},\"parentY\":{ParentY}}}");
        public void Draw(DesignCanvas canvas)=>Sprites.Draw(canvas,Asset,X,Y);
    }
    static float Snap(float value)=>MathF.Round(value*DesignCanvas.Px)/DesignCanvas.Px;
    public static Placement Island(string text,float centre,float groundY)
    {
        string asset="sign-"+(text switch {"HOME"=>"home","NURSERY"=>"nursery","TOY ROOM"=>"toy","GARDEN"=>"garden",_=>"cottage"});
        float x=Snap(centre),y=Snap(groundY);
        return new Placement(asset,x-Sprites.Size(asset).X/2,y-54,x,y-38);
    }
    public static Placement Perch(float centre,float deckY)
    {
        float x=Snap(centre),y=Snap(deckY);
        return new Placement("tag-perch",x-30,y+10,x,y);
    }
    public static void Recipient(DesignCanvas canvas,LevelDef level,bool delivered,float x,float y)
    {
        string asset=delivered?"tag-thanks":level.Package==CargoKind.Bottle?"tag-bottle":level.Package==CargoKind.Letter?"tag-letter":"tag-teddy";
        Sprites.Draw(canvas,asset,x,y);
    }
}
