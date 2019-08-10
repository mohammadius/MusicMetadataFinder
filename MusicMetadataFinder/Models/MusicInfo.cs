using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SpotifyAPI.Web.Models;

namespace MusicMetadataFinder.Models
{
    class MusicInfo
    {
        public Image Image { get; set; }
        public string Title { get; set; }
        public string Album { get; set; }
        public List<string> AlbumArtists { get; set; }
        public List<string> SongArtists { get; set; }
        public uint Year { get; set; }
        public uint TrackNumber { get; set; }
        public List<string> Genres { get; set; }

        private string _songUrl;
        public string SongUrl
        {
            get => _songUrl;
            set
            {
                //casting the url from something like this: spotify:track:05iXELwY3eDBBzXJvY2VAj
                //to this: https://open.spotify.com/track/05iXELwY3eDBBzXJvY2VAj

                var s = value.Split(':');
                _songUrl = $"https://open.spotify.com/{s[1]}/{s[2]}";
            }
        }

        public ImageSource ImageSource => UriToBitmapImage(Image.Url);

        public string GenresPreview
        {
            get
            {
                if (Genres.Count == 0)
                    return String.Empty;
                var str = String.Empty;
                if (Genres.Count > 1)
                {
                    for (int i = 0; i < Genres.Count - 1; i++)
                    {
                        str += Genres[i] + ", ";
                    }
                }
                str += Genres[Genres.Count - 1];
                return str;
            }
        }

        public string SongArtistsPreview
        {
            get
            {
                if (SongArtists.Count == 0)
                    return String.Empty;
                var str = String.Empty;
                if (SongArtists.Count > 1)
                {
                    for (int i = 0; i < SongArtists.Count - 1; i++)
                    {
                        str += SongArtists[i] + ", ";
                    }
                }
                str += SongArtists[SongArtists.Count - 1];
                return str;
            }
        }

        private BitmapImage UriToBitmapImage(string uri)
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(uri);
            bi.EndInit();
            return bi;
        }
    }
}
