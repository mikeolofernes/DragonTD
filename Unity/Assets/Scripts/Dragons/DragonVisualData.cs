using UnityEngine;

namespace DragonTD.Dragons
{
    [System.Serializable]
    public class DragonVisualData
    {
        [Header("Addressable Keys")]
        public string iconKey;
        public string portraitKey;
        public string[] stageSpritesKeys = new string[6];  // index = DragonEvolutionStage
        public string[] stagePrefabKeys  = new string[6];

        [Header("Colors")]
        public Color primaryColor   = Color.white;
        public Color secondaryColor = Color.gray;

        [Header("VFX Keys")]
        public string idleVfxKey;
        public string deathVfxKey;

        [Header("Prototype — Direct References (no Addressables required)")]
        public Sprite    portrait;
        public GameObject hatchlingPrefab;
    }
}
