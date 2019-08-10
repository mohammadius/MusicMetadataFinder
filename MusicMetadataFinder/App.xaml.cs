using System.IO;
using System.Windows;

namespace MusicMetadataFinder
{
    public partial class App : Application
    {
        public App()
        {
            var appDataPath = MyResources.Instance.AppDataPath;
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            else
            {
                //the path already exists and we don't need to do anything, EZ!
            }
        }
    }
}
