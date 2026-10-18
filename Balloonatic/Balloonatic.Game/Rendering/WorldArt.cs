using Balloonatic.Core;
namespace Balloonatic.Rendering;

static class WorldArt
{
    public static int Clock(SimState state)=>state.Complete?state.CompletedAtTick:state.Tick;
    public static int BurnerFrame(SimState state)=>!state.Heating?0:AppSettings.ReducedMotion?1:1+Clock(state)/8%2;
    public static int BirdFrame(SimState state)=>AppSettings.ReducedMotion?0:Clock(state)/8%2;
    public static int SockFrame(SimState state)=>AppSettings.ReducedMotion?0:Clock(state)/12%2;
    public static string Presentation(SimState state)=>"motion="+(AppSettings.ReducedMotion?1:0)+";burner="+BurnerFrame(state)+";bird="+BirdFrame(state)+";sock="+SockFrame(state)+";mill="+(AppSettings.ReducedMotion?0:Clock(state)/18%2);
    public static string Landmark(LevelDef level, bool launch, int tick=0) =>
        launch ? "cottage" : level.Id == "nursery-skies" ? "nursery-house" : level.Id == "winding-trail" ? "toy-blocks" : level.Id == "night-crossing" ? "garden-mill-"+(AppSettings.ReducedMotion?0:tick/18%2) : "cottage";
    public static string Perch(LevelDef level) => level.Id=="night-crossing" ? "garden-perch" : "island-perch";
    public static string Parcel(CargoKind kind) => kind == CargoKind.Bottle ? "parcel-bottle" :
        kind == CargoKind.Letter ? "parcel-letter" : "parcel-teddy";
    public static string Describe(SimState state)
    {
        LevelDef level=LevelCatalog.Get(state.LevelId);
        string landmark=Landmark(level,false), parcel=Parcel(level.Package);
        return "ready="+(Sprites.Ready && Sprites.Has(landmark) && Sprites.Has(parcel) && Sprites.Has("recipient-0") ? 1:0)
            +";slices="+Sprites.Count+";landmark="+landmark+";parcel="+parcel+";recipient="+(state.Delivered?"happy":"waiting");
    }
}
