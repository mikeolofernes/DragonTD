using UnityEngine;

namespace DragonTD.TowerDefense
{
    // Bundles a chapter's map layout and its ordered wave set.
    [CreateAssetMenu(fileName = "ChapterContent", menuName = "Dragon Dominion/Chapter Content")]
    public class ChapterContent : ScriptableObject
    {
        public int chapterNumber = 1;
        public MapDefinition map;
        public WaveData[] waves;

        // Set before scene managers initialize (by GameManager.Awake); null = use scene-assigned defaults.
        public static ChapterContent Active;
    }
}
