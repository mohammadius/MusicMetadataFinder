using System;
using System.IO;
using SpotifyAPI.Web;

namespace MusicMetadataFinder
{
    class MyResources
    {
        //a singleton class for resources to be used in the application

        private static readonly MyResources _instance = new MyResources();

        static MyResources()
        {
        }

        private MyResources()
        {
        }

        public static MyResources Instance => _instance;

        public readonly string AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MusicMetadataFinder");
        public readonly string TempPicPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"MusicMetadataFinder\pic.jpg");
        public readonly string TokenPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"MusicMetadataFinder\token.banana");
        public readonly string ClientId = "7862c79574984bcca273521b1478cdf5";
        public readonly string ClientSecret = "cb9b333f6445401aabeb8a6522c12217";
        public SpotifyWebAPI Api { get; set; }
    }
}