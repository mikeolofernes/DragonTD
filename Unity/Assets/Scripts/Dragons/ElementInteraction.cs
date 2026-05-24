namespace DragonTD.Dragons
{
    public static class ElementInteraction
    {
        private static readonly float[,] _table = BuildTable();

        private static float[,] BuildTable()
        {
            int n = System.Enum.GetValues(typeof(DragonElement)).Length;
            float[,] t = new float[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    t[i, j] = 1f;

            void Set(DragonElement a, DragonElement d, float v) => t[(int)a, (int)d] = v;

            Set(DragonElement.Fire,      DragonElement.Ice,       1.5f);
            Set(DragonElement.Fire,      DragonElement.Wind,      0.75f);
            Set(DragonElement.Fire,      DragonElement.Water,     0.75f);
            Set(DragonElement.Water,     DragonElement.Fire,      1.5f);
            Set(DragonElement.Water,     DragonElement.Earth,     1.5f);
            Set(DragonElement.Water,     DragonElement.Lightning, 0.75f);
            Set(DragonElement.Wind,      DragonElement.Earth,     1.5f);
            Set(DragonElement.Wind,      DragonElement.Lightning, 1.5f);
            Set(DragonElement.Lightning, DragonElement.Water,     1.5f);
            Set(DragonElement.Lightning, DragonElement.Wind,      0.75f);
            Set(DragonElement.Ice,       DragonElement.Wind,      1.5f);
            Set(DragonElement.Ice,       DragonElement.Fire,      0.75f);
            Set(DragonElement.Earth,     DragonElement.Lightning, 1.5f);
            Set(DragonElement.Earth,     DragonElement.Fire,      0.75f);
            Set(DragonElement.Shadow,    DragonElement.Light,     1.5f);
            Set(DragonElement.Light,     DragonElement.Shadow,    1.5f);

            return t;
        }

        public static float GetMultiplier(DragonElement attacker, DragonElement defender) =>
            _table[(int)attacker, (int)defender];
    }
}
