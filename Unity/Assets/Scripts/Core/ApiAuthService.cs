using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DragonTD.Core
{
    public class ApiAuthService
    {
        private readonly string _baseUrl;

        public ApiAuthService(string baseUrl)
        {
            _baseUrl = baseUrl;
        }

        public async Task<AuthSessionData> LoginWithDeviceAsync(string deviceId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                return null;

            var dto = new AuthRequestDto
            {
                device_id = deviceId,
                display_name = displayName
            };

            string json = JsonUtility.ToJson(dto);
            using (var request = new UnityWebRequest($"{_baseUrl}/api/v1/auth/device", UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                    return null;

                AuthResponseDto response = JsonUtility.FromJson<AuthResponseDto>(request.downloadHandler.text);
                return response != null && response.success ? response.data : null;
            }
        }
    }
}
