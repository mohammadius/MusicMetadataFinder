using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using MusicMetadataFinder.Models;
using Newtonsoft.Json;
using RestSharp;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using TagLib;

namespace MusicMetadataFinder
{
    public partial class MainWindow : Window
    {
        private void ConnectToSpotify()
        {
            /*calls the GetSpotifyWebAPI() method and if it goes well Spotify function of the app is enabled
             but if en exception is thrown Spotify will get disabled.
             the exceptions always get thrown so they all end up here*/

            try
            {
                GetSpotifyWebAPI();
                EnableSpotify();
            }
            catch (Exception e)
            {
                DisableSpotify(e.Message);
            }
        }

        private async void GetSpotifyWebAPI()
        {
            //using the token from TokenGetter() creates a new instance of SpotifyWebAPI

            try
            {
                var token = await TokenGetter();
                MyResources.Instance.Api = new SpotifyWebAPI { TokenType = token.TokenType, AccessToken = token.AccessToken };
            }
            catch (Exception)
            {
                throw;
            }
        }

        private Token GetToken()
        {
            /*using the ClientID and the ClientSecret gets a token from Spotify webAPI
             using RestSharp a HTTP POST request is sent to Spotify API sing their instruction from their developer documentation
             the answer from spotify api is json and using Newtonsoft.Json.JsonConvert it gets Deserialized to SpotifyAPI.Web.Models.Token*/

            var client = new RestClient("https://accounts.spotify.com/api/token");
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{MyResources.Instance.ClientId}:{MyResources.Instance.ClientSecret}"))}");
            request.AddHeader("content-type", "application/x-www-form-urlencoded");
            request.AddParameter("application/x-www-form-urlencoded", "grant_type=client_credentials", ParameterType.RequestBody);
            IRestResponse response = client.Execute(request);
            return JsonConvert.DeserializeObject<Token>(response.Content);
        }

        private async Task<Token> TokenGetter()
        {
            /*the ToKen gets serialized to json and saved for later used, the same token can be used to get a SpotifyWebAPI connection
             but there's a time limitation. and we check if the time hasn't passed we use that token
             if it has we just get another one*/

            try
            {
                SavedToken savedToken;
                Token token;
                if (System.IO.File.Exists(MyResources.Instance.TokenPath))
                {
                    using (var sr = new StreamReader(MyResources.Instance.TokenPath,Encoding.UTF8))
                    {
                        savedToken = JsonConvert.DeserializeObject<SavedToken>(await sr.ReadToEndAsync());
                    }
                    if (savedToken.ExpiresInMinutes < 5)
                    {
                        token = GetToken();
                        if (token.HasError())
                        {
                            throw new Exception(token.ErrorDescription);
                        }
                        using (var sw = new StreamWriter(MyResources.Instance.TokenPath, false, Encoding.UTF8))
                        {
                            savedToken = new SavedToken
                            {
                                TokenType = token.TokenType,
                                AccessToken = token.AccessToken,
                                ExpiresIn = token.ExpiresIn,
                                CreateDate = DateTime.Now
                            };
                            await sw.WriteAsync(JsonConvert.SerializeObject(savedToken, Formatting.Indented));
                        }
                    }
                    else
                    {
                        token = new Token
                        {
                            TokenType = savedToken.TokenType,
                            AccessToken = savedToken.AccessToken,
                            ExpiresIn = savedToken.ExpiresIn,
                            CreateDate = savedToken.CreateDate
                        };
                    }
                }
                else
                {
                    token = GetToken();
                    if (token.HasError())
                    {
                        throw new Exception(token.ErrorDescription);
                    }
                    using (var sw = new StreamWriter(MyResources.Instance.TokenPath, false, Encoding.UTF8))
                    {
                        savedToken = new SavedToken
                        {
                            TokenType = token.TokenType,
                            AccessToken = token.AccessToken,
                            ExpiresIn = token.ExpiresIn,
                            CreateDate = DateTime.Now
                        };
                        await sw.WriteAsync(JsonConvert.SerializeObject(savedToken, Formatting.Indented));
                    }
                }
                return token;
            }
            catch (Exception e)
            {
                if (System.IO.File.Exists(MyResources.Instance.TokenPath))
                {
                    System.IO.File.Delete(MyResources.Instance.TokenPath);
                }

                throw;
            }
        }

        private async void Search()
        {
            //calling the search method and putting the search answers list into listBox Items

            try
            {
                //changing the button to show the user the search is happening
                _searching = true;
                SearchBtn.IsEnabled = false;
                SearchBtn.Content = "Searching...";

                var search = await Search(SearchTextBox.Text, SearchType.Track);
                if (search == null || search.HasError())
                {
                    throw new Exception(search?.Error.Message);
                }

                //saving FullAlbum to get them from their id to make less HTTP calls
                var idToAlbum = new Dictionary<string, FullAlbum>();

                /*in version 8 of C# the task below could be implemented await foreach
                 it would have better performance and smoother ui*/
                var musicList = new List<MusicInfo>();
                await Task.Run(() =>
                {
                    foreach (var track in search.Tracks.Items)
                    {
                        FullAlbum album;
                        if (idToAlbum.ContainsKey(track.Album.Id))
                        {
                            album = idToAlbum[track.Album.Id];
                        }
                        else
                        {
                            album = AlbumSearch(track.Album.Id);
                            idToAlbum.Add(track.Album.Id, album);
                        }
                        musicList.Add(new MusicInfo
                        {
                            Image = track.Album.Images[0],
                            Title = track.Name,
                            Album = album.Name,
                            AlbumArtists = album.Artists.Select(a => a.Name).ToList(),
                            SongArtists = track.Artists.Select(a => a.Name).ToList(),
                            Year = Convert.ToUInt32(album.ReleaseDate.Substring(0, 4)),
                            TrackNumber = Convert.ToUInt32(track.TrackNumber),
                            Genres = album.Genres,
                            SongUrl = track.Uri
                        });
                    }
                });
                listBox.ItemsSource = musicList;

                //the search has ended
                _searching = false;
                SearchBtn.IsEnabled = true;
                SearchBtn.Content = "Search";
            }
            catch (Exception e)
            {
                _searching = false;
                DisableSpotify(e.Message);
            }
        }

        private Task<SearchItem> Search(string q, SearchType type, int limit = 50, int offset = 0)
        {
            //just changing every 'space' to '+' and calling the Async version of the API Search method

            try
            {
                return MyResources.Instance.Api.SearchItemsAsync(q.Replace(" ", "+"), type, limit, offset);
            }
            catch
            {
                return null;
            }
        }

        private FullAlbum AlbumSearch(string albumId)
        {
            //this is used to get album name for each song

            var client = new RestClient($"https://api.spotify.com/v1/albums/{albumId}");
            var request = new RestRequest(Method.GET);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("authorization", $"{MyResources.Instance.Api.TokenType} {MyResources.Instance.Api.AccessToken}");
            var response = client.Execute(request);
            return JsonConvert.DeserializeObject<FullAlbum>(response.Content);
        }

        private BitmapImage IPictureToBitmapImage(IPicture picture)
        {
            var bi = new BitmapImage();
            using (var ms = new MemoryStream(picture.Data.Data))
            {
                ms.Position = 0;
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.StreamSource = ms;
                bi.EndInit();
            }
            return bi;
        }

        private IPicture ImageToIPicture(SpotifyAPI.Web.Models.Image image)
        {
            DownloadFileFromUri(image.Url, MyResources.Instance.TempPicPath);
            var picture =
                new Picture(new ByteVector((byte[])new ImageConverter()
                .ConvertTo(System.Drawing.Image.FromFile(MyResources.Instance.TempPicPath), typeof(byte[]))))
                { Type = PictureType.FrontCover, MimeType = "Cover" };
            return picture;
        }

        private void DownloadFileFromUri(string uri, string filePath)
        {
            using (var wc = new WebClient())
            {
                wc.DownloadFile(uri, filePath);
            }
        }

        private void DisableSpotify(string error)
        {
            //disabling the Spotify ui functions and deleting the saved token file

            if (System.IO.File.Exists(MyResources.Instance.TokenPath))
            {
                System.IO.File.Delete(MyResources.Instance.TokenPath);
            }

            MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            SearchBtn.Content = "Retry Connection";
            SearchBtn.IsEnabled = true;
            SpotifyBtn.IsEnabled = false;
            SearchTextBox.IsEnabled = false;
            listBox.ItemsSource = null;
        }

        private void EnableSpotify()
        {
            //enabling Spotify ui functions

            SearchBtn.Content = "Search";
            _searching = false;
            SearchBtn.IsEnabled = true;
            SpotifyBtn.IsEnabled = true;
            SearchTextBox.IsEnabled = true;
        }

        [DllImport("shell32.dll", EntryPoint = "ShellExecute")]
        public static extern long ShellExecute(int hwnd, string cmd, string file, string param1, string param2, int swmode);
    }
}