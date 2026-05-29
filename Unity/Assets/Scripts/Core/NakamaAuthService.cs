using System.Threading.Tasks;
using Nakama;
using UnityEngine;

namespace DragonTD.Core
{
    // Wraps Nakama device authentication.
    public class NakamaAuthService
    {
        private readonly NakamaConfig _config;
        public IClient Client { get; private set; }
        public ISession Session { get; private set; }
        public bool IsAuthenticated => Session != null && !Session.IsExpired;

        public NakamaAuthService(NakamaConfig config)
        {
            _config = config;
            Client = new Client(_config.scheme, _config.host, _config.port, _config.serverKey, UnityWebRequestAdapter.Instance, autoRefreshSession: true);
        }

        public async Task<bool> AuthenticateDeviceAsync(string deviceId)
        {
            try
            {
                Session = await Client.AuthenticateDeviceAsync(deviceId);
                return IsAuthenticated;
            }
            catch (ApiResponseException ex)
            {
                Debug.LogWarning($"[Nakama] Auth failed: {ex.Message}");
                return false;
            }
        }
    }
}
