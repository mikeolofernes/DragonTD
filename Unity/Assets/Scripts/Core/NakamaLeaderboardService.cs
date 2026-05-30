using System.Threading.Tasks;
using Nakama;
using UnityEngine;

namespace DragonTD.Core
{
    // Submits and lists leaderboard scores via Nakama.
    public class NakamaLeaderboardService
    {
        private readonly IClient _client;
        private readonly ISession _session;

        public NakamaLeaderboardService(IClient client, ISession session)
        {
            _client = client;
            _session = session;
        }

        public async Task<bool> SubmitScoreAsync(string leaderboardId, long score)
        {
            if (_client == null || _session == null) return false;
            try
            {
                await _client.WriteLeaderboardRecordAsync(_session, leaderboardId, score);
                return true;
            }
            catch (System.Exception ex) // ApiResponseException + network/HTTP failures on mobile
            {
                Debug.LogWarning($"[Nakama] Score submit failed: {ex.Message}");
                return false;
            }
        }

        public async Task<IApiLeaderboardRecordList> ListTopAsync(string leaderboardId, int limit)
        {
            if (_client == null || _session == null) return null;
            try
            {
                return await _client.ListLeaderboardRecordsAsync(_session, leaderboardId, ownerIds: null, expiry: null, limit, cursor: null);
            }
            catch (System.Exception ex) // ApiResponseException + network/HTTP failures on mobile
            {
                Debug.LogWarning($"[Nakama] List scores failed: {ex.Message}");
                return null;
            }
        }
    }
}
