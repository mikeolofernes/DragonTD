using System.Threading.Tasks;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace DragonTD.Core
{
    public class ApiProgressionPersistenceService : IProgressionPersistenceService
    {
        private readonly string _baseUrl;
        private readonly AuthSessionData _session;

        public string ModeLabel => _session != null && _session.IsAuthenticated ? "API" : "API Auth Required";

        public ApiProgressionPersistenceService(string baseUrl, AuthSessionData session = null)
        {
            _baseUrl = baseUrl;
            _session = session;
        }

        public Task<ProgressionPersistenceResult> LoadProgressionAsync()
        {
            return SendAsync<PlayerProgressionSaveData>($"{_baseUrl}/api/v1/progression", UnityWebRequest.kHttpVerbGET, null);
        }

        public Task<ProgressionPersistenceResult> SaveProgressionAsync(PlayerProgressionSaveData saveData)
        {
            return SendAsync<PlayerProgressionSaveData>($"{_baseUrl}/api/v1/progression", UnityWebRequest.kHttpVerbPUT, saveData);
        }

        public Task<ProgressionPersistenceResult> SyncBattleRewardAsync(PlayerProgressionApiDto progression, BattleRewardResult reward)
        {
            return SendAsync<PlayerProgressionApiDto>($"{_baseUrl}/api/v1/progression/battle-rewards", UnityWebRequest.kHttpVerbPOST, progression);
        }

        public void ResetProgression()
        {
        }

        private async Task<ProgressionPersistenceResult> SendAsync<T>(string url, string method, T payload)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                return ProgressionPersistenceResult.Failure("API base URL is not configured");
            if (_session == null || !_session.IsAuthenticated)
                return ProgressionPersistenceResult.Failure("API auth session is not available");

            using (var request = new UnityWebRequest(url, method))
            {
                if (payload != null)
                {
                    string json = JsonUtility.ToJson(payload);
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json");
                }

                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Authorization", $"Bearer {_session.accessToken}");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                    return ProgressionPersistenceResult.Failure(request.error);

                if (method == UnityWebRequest.kHttpVerbGET && !string.IsNullOrWhiteSpace(request.downloadHandler.text))
                {
                    PlayerProgressionSaveData saveData = JsonUtility.FromJson<PlayerProgressionSaveData>(request.downloadHandler.text);
                    return ProgressionPersistenceResult.Success("API loaded", saveData);
                }

                return ProgressionPersistenceResult.Success("API synced");
            }
        }
    }
}
