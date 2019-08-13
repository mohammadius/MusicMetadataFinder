using System;
using Newtonsoft.Json;

namespace MusicMetadataFinder.Models
{
    class SavedToken
    {
        [JsonProperty("TokenType")]
        public string TokenType { get; set; }

        [JsonProperty("AccessToken")]
        public string AccessToken { get; set; }

        [JsonProperty("ExpiresIn")]
        public double ExpiresIn { get; set; }

        [JsonProperty("CreateDate")]
        public DateTime CreateDate { get; set; }

        [JsonIgnore]
        public bool IsExpired => CreateDate.Add(TimeSpan.FromSeconds(ExpiresIn)) <= DateTime.Now;

        [JsonIgnore]
        public int ExpiresInMinutes => (int)(CreateDate.Add(TimeSpan.FromSeconds(ExpiresIn)) - DateTime.Now).TotalMinutes;
    }
}
