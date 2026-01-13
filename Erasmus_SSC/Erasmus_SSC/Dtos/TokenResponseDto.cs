using System.Text.Json.Serialization;

namespace Erasmus_SSC.Dtos
{
   
        public class TokenResponseDto
        {
            [JsonPropertyName("accessToken")]
            public string AccessToken { get; set; } = string.Empty;
            [JsonPropertyName("refreshToken")]
            public string? RefreshToken { get; set; }
        }
    
}
