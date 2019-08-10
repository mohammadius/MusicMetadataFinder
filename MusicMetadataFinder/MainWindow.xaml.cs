using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using MusicMetadataFinder.Extensions;
using MusicMetadataFinder.Models;

namespace MusicMetadataFinder
{
    public partial class MainWindow : Window
    {
        public TagLib.File Song { get; set; }
        public SpotifyAPI.Web.Models.Image SpotifyImage { get; set; }
        public ImageSource OldImageSource = null;

        private string _songFilePos;
        private bool _searching = false;

        public MainWindow()
        {
            InitializeComponent();
            ConnectToSpotify();
            bgRectangle.Fill = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            /*Window background is set to "VisualBrush". and in the "Visual" property of that there's this Rectangle shape
             and using the "Fill" property we change the windows background to either a solid color or an image*/
        }

        private void OpenBtn_OnClick(object sender, RoutedEventArgs e)
        {
            //handles opening a file (song) to later edit its metadata
            //reading the files metadata and if they are not null showing them

            Song?.Dispose();
            SpotifyImage = null;

            var ofd = new OpenFileDialog
            {
                Multiselect = false, //can only select one song to edit
                //limiting file selection to supported formats
                Filter = "Music Files (*.mp3,*.wav,etc.)|*.aa;*.aax;*.aac;*.aiff;*.ape;*.dsf;*.flac;*.m4a;*.m4b;*.m4p;*.mp3;*.mpc;*.mpp;*.ogg;*.oga;*.wav;*.wma;*.wv;*.webm"
            };
            if (ofd.ShowDialog().GetValueOrDefault(false))
            {
                Song = TagLib.File.Create(ofd.FileName);
            }
            else
            {
                return; //returning if OpenFileDialog is cancelled
            }

            _songFilePos = ofd.FileName;

            //In version 8 of C# this sentence could be shorted to: FileNameTextBox.Text = ofd.FileName.Split('\\')[^1];
            FileNameTextBox.Text = ofd.FileName.Split('\\')[ofd.FileName.Split('\\').Length - 1];

            //could have used ?: expression but that wouldn't be so future proofing! (i'm not going to comment the same text for all of this type of things)
            if (!string.IsNullOrWhiteSpace(Song.Tag.Title))
            {
                TitleTextBox.Text = Song.Tag.Title;
            }
            else
            {
                TitleTextBox.Text = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(Song.Tag.Album))
            {
                AlbumTextBox.Text = Song.Tag.Album;
            }
            else
            {
                AlbumTextBox.Text = string.Empty;
            }

            if (Song.Tag.Performers != null && Song.Tag.Performers.Length != 0)
            {
                var str = string.Empty;
                if (Song.Tag.Performers.Length > 1)
                {
                    for (int i = 0; i < Song.Tag.Performers.Length - 1; i++)
                    {
                        str += Song.Tag.Performers[i] + ", ";
                    }
                }

                str += Song.Tag.Performers[Song.Tag.Performers.Length - 1];

                SongArtistsTextBox.Text = str;
            }
            else
            {
                SongArtistsTextBox.Text = string.Empty;
            }

            if (Song.Tag.AlbumArtists != null && Song.Tag.AlbumArtists.Length != 0)
            {
                var str = string.Empty;
                if (Song.Tag.AlbumArtists.Length > 1)
                {
                    for (int i = 0; i < Song.Tag.AlbumArtists.Length - 1; i++)
                    {
                        str += Song.Tag.AlbumArtists[i] + ", ";
                    }
                }

                str += Song.Tag.AlbumArtists[Song.Tag.AlbumArtists.Length - 1];

                AlbumArtistsTextBox.Text = str;
            }
            else
            {
                AlbumArtistsTextBox.Text = string.Empty;
            }

            if (Song.Tag.Year > 0)
            {
                YearTextBox.Text = Song.Tag.Year.ToString();
            }
            else
            {
                YearTextBox.Text = string.Empty;
            }

            if (Song.Tag.Track > 0)
            {
                TrackNumberTextBox.Text = Song.Tag.Track.ToString();
            }
            else
            {
                TrackNumberTextBox.Text = string.Empty;
            }

            //using dynamic type to use the same variable as "From" for the background changing animation
            //and yes, i used ?: expression here. could have used if else statement but "too much effort"!
            var brush = OldImageSource != null
                ? (dynamic)new ImageBrush(OldImageSource)
                : new SolidColorBrush(Color.FromRgb(30, 30, 30));
            if (Song.Tag.Pictures != null && Song.Tag.Pictures.Length != 0)
            {
                image.ImageSource = IPictureToBitmapImage(Song.Tag.Pictures[0]); //changing format of the TagLib picture to BitmapImage

                bgRectangle.BeginAnimation(Shape.FillProperty, new BrushAnimation
                {
                    Duration = new Duration(TimeSpan.FromSeconds(2)),
                    From = brush,
                    To = new ImageBrush(image.ImageSource)
                });
                OldImageSource = image.ImageSource;
            }
            else
            {
                OldImageSource = null;
                image.ImageSource = new BitmapImage(new Uri(@"PlaceholderImage.png", UriKind.Relative));
                bgRectangle.BeginAnimation(Shape.FillProperty, new BrushAnimation
                {
                    Duration = new Duration(TimeSpan.FromSeconds(2)),
                    From = brush,
                    To = new SolidColorBrush(Color.FromRgb(30, 30, 30))
                });
            }

            if (Song.Tag.Genres != null && Song.Tag.Genres.Length != 0)
            {
                var str = string.Empty;
                if (Song.Tag.Genres.Length > 1)
                {
                    for (int i = 0; i < Song.Tag.Genres.Length - 1; i++)
                    {
                        str += Song.Tag.Genres[i] + ", ";
                    }
                }

                str += Song.Tag.Genres[Song.Tag.Genres.Length - 1];

                GenresTextBox.Text = str;
            }
            else
            {
                GenresTextBox.Text = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(Song.Tag.Lyrics))
            {
                LyricTextBox.Text = Song.Tag.Lyrics;
            }
            else
            {
                LyricTextBox.Text = string.Empty;
            }
        }

        private void Spotify_OnClick(object sender, RoutedEventArgs e)
        {
            //places the available metadata from Spotify selected music

            if (listBox.SelectedItems.Count == 0)
            {
                return; //returning if no song is selected
            }

            //selecting first item selected. because "SelectionMode" property on ListBox is set to Single we know it's the only and the actual selected item
            var selectedMusic = (MusicInfo)listBox.SelectedItems[0];

            if (selectedMusic.Title != null)
            {
                TitleTextBox.Text = selectedMusic.Title;
            }

            if (selectedMusic.Album != null)
            {
                AlbumTextBox.Text = selectedMusic.Album;
            }

            if (selectedMusic.SongArtists != null && selectedMusic.SongArtists.Count != 0)
            {
                var str = string.Empty;
                if (selectedMusic.SongArtists.Count > 1)
                {
                    for (int i = 0; i < selectedMusic.SongArtists.Count - 1; i++)
                    {
                        str += selectedMusic.SongArtists[i] + ", ";
                    }
                }

                str += selectedMusic.SongArtists[selectedMusic.SongArtists.Count - 1];

                SongArtistsTextBox.Text = str;
            }

            if (selectedMusic.AlbumArtists != null && selectedMusic.AlbumArtists.Count != 0)
            {
                var str = string.Empty;
                if (selectedMusic.AlbumArtists.Count > 1)
                {
                    for (int i = 0; i < selectedMusic.AlbumArtists.Count - 1; i++)
                    {
                        str += selectedMusic.AlbumArtists[i] + ", ";
                    }
                }

                str += selectedMusic.AlbumArtists[selectedMusic.AlbumArtists.Count - 1];

                AlbumArtistsTextBox.Text = str;
            }

            if (selectedMusic.Year > 0)
            {
                YearTextBox.Text = selectedMusic.Year.ToString();
            }

            if (selectedMusic.TrackNumber > 0)
            {
                TrackNumberTextBox.Text = selectedMusic.TrackNumber.ToString();
            }

            //read line 132
            var brush = OldImageSource != null
                ? (dynamic)new ImageBrush(OldImageSource)
                : new SolidColorBrush(Color.FromRgb(30, 30, 30));
            SpotifyImage = null;
            if (selectedMusic.Image != null)
            {
                image.ImageSource = selectedMusic.ImageSource;
                SpotifyImage = selectedMusic.Image;
                bgRectangle.BeginAnimation(Shape.FillProperty, new BrushAnimation
                {
                    Duration = new Duration(TimeSpan.FromSeconds(2)),
                    From = brush,
                    To = new ImageBrush(image.ImageSource)
                });
                OldImageSource = selectedMusic.ImageSource;
            }

            if (selectedMusic.Genres != null && selectedMusic.Genres.Count != 0)
            {
                var str = string.Empty;
                if (selectedMusic.Genres.Count > 1)
                {
                    for (int i = 0; i < selectedMusic.Genres.Count - 1; i++)
                    {
                        str += selectedMusic.Genres[i] + ", ";
                    }
                }

                str += selectedMusic.Genres[selectedMusic.Genres.Count - 1];

                GenresTextBox.Text = str;
            }
        }

        private void SearchBtn_OnClick(object sender, RoutedEventArgs e)
        {
            //if listBox is disabled it means there was connection issues and the button is changed to retry connection
            if (SearchTextBox.IsEnabled)
            {
                Search();
            }
            else
            {
                ConnectToSpotify();
            }
        }

        private void SaveBtn_OnClick(object sender, RoutedEventArgs e)
        {
            //changing metadata on the opened file and saving it

            if (Song == null || Song.Writeable == false)
            {
                return; //returning if no file is opened or the file isn't writable
            }

            if (!string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                Song.Tag.Title = TitleTextBox.Text;
            }
            else
            {
                Song.Tag.Title = null;
            }

            if (!string.IsNullOrWhiteSpace(AlbumTextBox.Text))
            {
                Song.Tag.Album = AlbumTextBox.Text;
            }
            else
            {
                Song.Tag.Album = null;
            }

            if (!string.IsNullOrWhiteSpace(SongArtistsTextBox.Text))
            {
                var artists = SongArtistsTextBox.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (artists.Length == 0) return;
                IEnumerable<string> a = new[] { artists[0].Trim() };
                for (var i = 1; i < artists.Length; i++)
                {
                    a = a.Concat(new[] { artists[i].Trim() });
                }

                Song.Tag.Performers = a.ToArray();
            }
            else
            {
                Song.Tag.Performers = null;
            }

            if (!string.IsNullOrWhiteSpace(AlbumArtistsTextBox.Text))
            {
                var artists = AlbumArtistsTextBox.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (artists.Length == 0) return;
                IEnumerable<string> a = new[] { artists[0].Trim() };
                for (var i = 1; i < artists.Length; i++)
                {
                    a = a.Concat(new[] { artists[i].Trim() });
                }

                Song.Tag.AlbumArtists = a.ToArray();
            }
            else
            {
                Song.Tag.AlbumArtists = null;
            }

            if (!string.IsNullOrWhiteSpace(YearTextBox.Text))
            {
                Song.Tag.Year = Convert.ToUInt32(YearTextBox.Text);
            }
            else
            {
                Song.Tag.Year = 0;
            }

            if (!string.IsNullOrWhiteSpace(TrackNumberTextBox.Text))
            {
                Song.Tag.Track = Convert.ToUInt32(TrackNumberTextBox.Text);
            }
            else
            {
                Song.Tag.Track = 0;
            }

            if (SpotifyImage != null)
            {
                Song.Tag.Pictures = new[] { ImageToIPicture(SpotifyImage) };
            }

            if (!string.IsNullOrWhiteSpace(GenresTextBox.Text))
            {
                var genres = GenresTextBox.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (genres.Length == 0) return;
                IEnumerable<string> a = new[] { genres[0].Trim() };
                for (var i = 1; i < genres.Length; i++)
                {
                    a = a.Concat(new[] { genres[i].Trim() });
                }

                Song.Tag.Genres = a.ToArray();
            }
            else
            {
                Song.Tag.Genres = null;
            }

            if (!string.IsNullOrWhiteSpace(LyricTextBox.Text))
            {
                Song.Tag.Lyrics = LyricTextBox.Text;
            }
            else
            {
                Song.Tag.Lyrics = null;
            }

            Song.Save();
        }

        private void SearchTextBox_OnKeyUp(object sender, KeyEventArgs e)
        {
            //if Enter key is pressed on the search TextBox and we're not already searching then call the Search()
            if (e.Key == Key.Enter && !_searching)
            {
                Search();
            }
        }

        private void Image_OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (Song != null)
            {
                playSongImage.Opacity = 1;
            }
        }

        private void Image_OnMouseLeave(object sender, MouseEventArgs e)
        {
            playSongImage.Opacity = 0;
        }

        private void PlaySongImage_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //play song using windows ShellExecute

            if (Song != null)
            {
                ShellExecute(0, "open", _songFilePos, "", "", 5);
            }
        }

        private void UIElement_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //user can copy the song URL to clipboard by right clicking the item in listBox

            Clipboard.SetText((listBox.SelectedItems[0] as MusicInfo)?.SongUrl ?? string.Empty);

            var toolTip = new ToolTip
            {
                Content = "Copied the Spotify song URL to clipboard",
                StaysOpen = false,
                IsOpen = true,
                Background = new SolidColorBrush(Color.FromRgb(30,30,30))
            };

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3),
                IsEnabled = true
            };

            timer.Tick += delegate
            {
                if (toolTip != null)
                {
                    toolTip.IsOpen = false;
                }
                toolTip = null;
                timer = null;
            };
        }
    }
}