using System.IO;
using System.Threading.Tasks;

namespace Reckoner.Utilities
{

  public static class FileUtils
  {
    public static Stream OpenFile(string filePath, bool isMauiEnvironment = true)
    {

      //if (PlatformUtils.IsMauiBuild)
      //{
      //  // MAUI: Run async file open synchronously within the context of the task
      //  FileSystem.AppPackageFileExistsAsync(filePath).Wait();
      //  return Task.Run(() => FileSystem.OpenAppPackageFileAsync(filePath)).GetAwaiter().GetResult();
      //}
      //else
      {
        // Non-MAUI: Use synchronous file open directly
        return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
      }
    }
  }

}
