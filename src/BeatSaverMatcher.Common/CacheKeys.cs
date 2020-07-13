namespace BeatSaverMatcher.Common
{
    public class CacheKeys
    {
        public static string GetForBeatmapStats(int key)
        {
            return $"BeatmapStats_{key}";
        }
    }
}
