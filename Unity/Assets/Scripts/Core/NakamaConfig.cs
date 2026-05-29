using UnityEngine;

namespace DragonTD.Core
{
    // Dev defaults point at a local Nakama server. Do NOT commit production keys.
    [CreateAssetMenu(fileName = "NakamaConfig", menuName = "Dragon Dominion/Nakama Config")]
    public class NakamaConfig : ScriptableObject
    {
        public string scheme = "http";
        public string host = "127.0.0.1";
        public int port = 7350;
        public string serverKey = "defaultkey";
        public string battleLeaderboardId = "battle_score";
    }
}
