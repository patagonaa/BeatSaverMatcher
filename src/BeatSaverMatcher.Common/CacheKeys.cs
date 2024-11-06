namespace BeatSaverMatcher.Common
{
    public class CacheKeys
    {
        public static string GetForBeatmap(int key)
        {
            return $"Beatmap_{key}";
        }
    }
}
