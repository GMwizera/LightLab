using UnityEngine;

namespace PassTheLight.Core
{
    public static class ProgressStore
    {
        const string UnlockedKey = "ptl_unlocked";
        const string BestMovesKey = "ptl_best_";
        const string BestStarsKey = "ptl_stars_";

        public static int UnlockedLevels => PlayerPrefs.GetInt(UnlockedKey, 1);
        public static int BestMoves(int level) => PlayerPrefs.GetInt(BestMovesKey + level, 0);
        public static int BestStars(int level) => PlayerPrefs.GetInt(BestStarsKey + level, 0);

        public static void Unlock(int levelCount)
        {
            if (levelCount <= UnlockedLevels) return;
            PlayerPrefs.SetInt(UnlockedKey, levelCount);
            PlayerPrefs.Save();
        }

        public static bool TrySaveBest(int level, int moves)
        {
            int best = BestMoves(level);
            bool isNewBest = best == 0 || moves < best;
            if (!isNewBest) return false;

            PlayerPrefs.SetInt(BestMovesKey + level, moves);
            PlayerPrefs.Save();
            return true;
        }

        public static void SaveStars(int level, int stars)
        {
            if (stars <= BestStars(level)) return;
            PlayerPrefs.SetInt(BestStarsKey + level, stars);
            PlayerPrefs.Save();
        }

        public static void ResetProgress(int levelCount)
        {
            PlayerPrefs.DeleteKey(UnlockedKey);
            for (int level = 0; level < levelCount; level++)
            {
                PlayerPrefs.DeleteKey(BestMovesKey + level);
                PlayerPrefs.DeleteKey(BestStarsKey + level);
            }
            PlayerPrefs.Save();
        }
    }
}
