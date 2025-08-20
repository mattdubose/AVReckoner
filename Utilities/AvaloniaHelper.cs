using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Utilities
{
    public class AvaloniaHelper
    {
        const string ASSETS_DIR = "Assets";
        public static string GetWritableDatabasePath(string appName, string dBFileName)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbDirectory = Path.Combine(appData, appName);
            Directory.CreateDirectory(dbDirectory);

            var targetPath = Path.Combine(dbDirectory, dBFileName);
            var sourcePath = Path.Combine(AppContext.BaseDirectory, ASSETS_DIR, dBFileName);

            if (!File.Exists(targetPath))
            {
                File.Copy(sourcePath, targetPath);
            }

            return targetPath;
        }

    }
}
