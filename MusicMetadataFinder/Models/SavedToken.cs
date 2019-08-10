using System;

namespace MusicMetadataFinder.Models
{
    [Serializable]
    class SavedToken
    {
        public string TokenType { get; set; }
        public string AccessToken { get; set; }
        public double ExpiresIn { get; set; }
        public DateTime CreateDate { get; set; }

        public bool IsExpired => CreateDate.Add(TimeSpan.FromSeconds(ExpiresIn)) <= DateTime.Now;
        public int ExpiresInMinutes => (int)(CreateDate.Add(TimeSpan.FromSeconds(ExpiresIn)) - DateTime.Now).TotalMinutes;
    }
}
