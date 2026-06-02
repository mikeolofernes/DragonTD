using System;

namespace DragonTD.Core
{
    [Serializable]
    public class AuthSessionData
    {
        public string playerId;
        public string accessToken;
        public string refreshToken;
        public long expiresUtcTicks;

        public bool IsAuthenticated =>
            !string.IsNullOrWhiteSpace(accessToken) && expiresUtcTicks > DateTime.UtcNow.Ticks;
    }

    [Serializable]
    public class AuthRequestDto
    {
        public string device_id;
        public string display_name;
    }

    [Serializable]
    public class AuthResponseDto
    {
        public bool success;
        public AuthSessionData data;
        public string error;
    }
}
