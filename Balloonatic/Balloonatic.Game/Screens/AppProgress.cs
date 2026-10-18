using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Balloonatic.Core;
namespace Balloonatic.Screens {
    // Player records belong to the application, never to the deterministic simulation.
    static class AppProgress {
        static readonly Dictionary<string,int> BestStars=new(StringComparer.Ordinal);
        public static int Count=>BestStars.Count;
        public static void Mark(string id,int stars) {
            if(!LevelCatalog.TryGet(id,out _)||stars<1||stars>3)return;
            BestStars.TryGetValue(id,out int best);BestStars[id]=Math.Max(best,stars);
        }
        public static bool Delivered(string id)=>BestStars.ContainsKey(id);
        public static int StarsFor(string id)=>BestStars.TryGetValue(id,out int stars)?stars:0;
        public static string Save() {
            var text=new StringBuilder("version=1;").Append(AppSettings.Describe());
            foreach(LevelDef level in LevelCatalog.Levels)text.Append(';').Append(level.Id).Append('=').Append(StarsFor(level.Id));
            return text.ToString();
        }
        public static bool Restore(string text) {
            if(text.Length>1024)return false;
            var values=new Dictionary<string,int>(StringComparer.Ordinal);
            foreach(string pair in text.Split(';')) {
                string[] parts=pair.Split('=');
                if(parts.Length!=2||!int.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out int value)||!values.TryAdd(parts[0],value))return false;
            }
            if(values.Count!=5+LevelCatalog.Levels.Length||!values.TryGetValue("version",out int version)||version!=1)return false;
            foreach(var pair in values) {
                int max=pair.Key=="version"?1:pair.Key is "music" or "sfx"?10:pair.Key is "assist" or "motion"?1:LevelCatalog.TryGet(pair.Key,out _)?3:-1;
                if(pair.Value<0||pair.Value>max)return false;
            }
            foreach(string key in new[]{"music","sfx","assist","motion"})if(!values.ContainsKey(key))return false;
            foreach(LevelDef level in LevelCatalog.Levels)if(!values.ContainsKey(level.Id))return false;
            BestStars.Clear();foreach(LevelDef level in LevelCatalog.Levels)if(values[level.Id]>0)Mark(level.Id,values[level.Id]);
            AppSettings.MusicVolume=values["music"];AppSettings.SoundVolume=values["sfx"];
            AppSettings.WindAssist=values["assist"]==1;AppSettings.ReducedMotion=values["motion"]==1;
            return true;
        }
    }
}
