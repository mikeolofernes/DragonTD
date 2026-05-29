using System;
using System.Collections.Generic;

namespace DragonTD.Core
{
    [Serializable]
    public class PlayerProgressionApiDto
    {
        public int version = 1;
        public int essence;
        public int gold;
        public int gems;
        public int summon_tickets;
        public int gacha_pulls_since_last_epic;
        public int gacha_total_pulls;
        public int damage_buff_level;
        public int attack_speed_buff_level;
        public int starting_mana_buff_level;
        public string current_stage_id;
        public int highest_unlocked_stage_index;
        public List<string> cleared_stage_ids = new();
        public List<StageStarApiDto> stage_stars = new();
        public List<string> equipped_dragon_ids = new();
        public List<ChestSlotApiDto> chest_slots = new();
        public List<EventClaimApiDto> event_claims = new();
        public List<DailyObjectiveApiDto> daily_objectives = new();
        public List<DragonProgressionApiDto> dragons = new();
        public BattleRewardApiDto last_battle_reward;
    }

    [Serializable]
    public class ChestSlotApiDto
    {
        public string rarity;
        public long unlock_complete_utc_ticks;
        public bool claimed;
    }

    [Serializable]
    public class EventClaimApiDto
    {
        public string event_id;
        public long last_claim_utc_ticks;
        public int claim_count;
    }

    [Serializable]
    public class DailyObjectiveApiDto
    {
        public string objective_id;
        public long date_utc_ticks;
        public int progress;
        public bool claimed;
    }

    [Serializable]
    public class StageStarApiDto
    {
        public string stage_id;
        public int best_stars;
    }

    [Serializable]
    public class DragonProgressionApiDto
    {
        public string dragon_id;
        public int level;
        public int bond_level;
        public float bond_xp;
        public long total_battles;
        public int evolution_stage;
        public int skill_level;
    }

    [Serializable]
    public class BattleRewardApiDto
    {
        public bool victory;
        public int waves_cleared;
        public int dragons_rewarded;
        public int total_bond_xp;
        public int bond_level_ups;
        public string stage_id;
        public string stage_title;
        public int stars_earned;
        public int best_stars;
        public bool first_clear;
        public string objective_summary;
        public int stage_bonus_gold;
        public int stage_bonus_essence;
        public int stage_bonus_gems;
        public int stage_bonus_summon_tickets;
        public bool chest_awarded;
        public int chest_slot_index;
        public string chest_rarity;
        public string summary;
        public List<DragonBattleRewardApiDto> dragon_rewards = new();
    }

    [Serializable]
    public class DragonBattleRewardApiDto
    {
        public string dragon_id;
        public string display_name;
        public int bond_xp_granted;
        public int bond_level_before;
        public int bond_level_after;
        public long total_battles;
    }
}
