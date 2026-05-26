#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using DragonTD.Dragons;

namespace DragonTD.Editor
{
    public static class DragonExportTool
    {
        [MenuItem("Dragon Dominion/Export Dragons to JSON")]
        public static void ExportAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:DragonDefinition");
            var dragons = new List<DragonDefinition>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<DragonDefinition>(path);
                if (def != null) dragons.Add(def);
            }

            var exportList = new List<DragonExportEntry>();
            foreach (var d in dragons)
            {
                exportList.Add(new DragonExportEntry
                {
                    dragonId    = d.dragonId,
                    displayName = d.displayName,
                    dragonClass = d.dragonClass.ToString(),
                    element     = d.element.ToString(),
                    rarity      = d.rarity.ToString(),
                    baseStats   = new StatsExport
                    {
                        hp           = d.baseStats.hp,
                        attack       = d.baseStats.attack,
                        attack_speed = d.baseStats.attackSpeed,
                        armor        = d.baseStats.armor,
                        magic_resist = d.baseStats.magicResist,
                        range        = d.baseStats.range,
                        mana         = d.baseStats.mana,
                        crit_chance  = d.baseStats.critChance
                    },
                    mana_cost      = d.manaCost,
                    normal_attack  = d.skillSet.normalAttack?.skillId,
                    active_skill   = d.skillSet.activeSkill?.skillId,
                    passive_skill  = d.skillSet.passiveSkill?.skillId,
                    ultimate_skill = d.skillSet.ultimateSkill?.skillId
                });
            }

            string json = JsonUtility.ToJson(new ExportWrapper { dragons = exportList.ToArray() }, prettyPrint: true);
            string outputPath = Path.Combine(Application.dataPath, "../game-design-docs/dragons_export.json");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, json);

            AssetDatabase.Refresh();
            Debug.Log($"[DragonExportTool] Exported {dragons.Count} dragons to {outputPath}");
            EditorUtility.DisplayDialog("Export Complete", $"Exported {dragons.Count} dragons.\n{outputPath}", "OK");
        }

        [System.Serializable] private class ExportWrapper { public DragonExportEntry[] dragons; }

        [System.Serializable]
        private class DragonExportEntry
        {
            public string dragonId, displayName, dragonClass, element, rarity;
            public StatsExport baseStats;
            public int mana_cost;
            public string normal_attack, active_skill, passive_skill, ultimate_skill;
        }

        [System.Serializable]
        private class StatsExport
        {
            public float hp, attack, attack_speed, armor, magic_resist, range, mana, crit_chance;
        }
    }
}
#endif
