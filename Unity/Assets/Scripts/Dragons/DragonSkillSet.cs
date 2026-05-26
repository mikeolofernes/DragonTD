namespace DragonTD.Dragons
{
    [System.Serializable]
    public class DragonSkillSet
    {
        public SkillDefinition normalAttack;
        public SkillDefinition activeSkill;
        public SkillDefinition passiveSkill;
        public SkillDefinition ultimateSkill;  // null until Bond level 6
    }
}
