using Balloonatic.Core;
namespace Balloonatic.Rendering;

static class WorldArt
{
    public static string Landmark(LevelDef level, bool launch) =>
        launch ? "cottage" : level.Id == "nursery-skies" ? "nursery-house" : level.Id == "winding-trail" ? "toy-blocks" : "cottage";
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
