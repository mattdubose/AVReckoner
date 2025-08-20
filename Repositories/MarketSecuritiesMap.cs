using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reckoner.Utilities;
namespace Reckoner.Repositories
{
  public class MarketSecuritiesMap
  {
    static private string _mapFile = "BarChartsFileMap.csv"; 
    public MarketSecuritiesMap(string mapFile) 
    {
      _mapFile = mapFile;
    }
    public MarketSecuritiesMap() { }
    static public string GetSecuritiesFile(string TickerSymbol)
    {
      var stream = FileUtils.OpenFile(_mapFile);
      using var reader = new StreamReader(stream);

      // Read each line in the file
      while (!reader.EndOfStream)
      {
        var line = reader.ReadLine();

        if (line == null) continue;

        // Split line by comma, assuming only two columns
        var columns = line.Split(',');

        if (columns.Length < 2) continue; // Skip any malformed lines

        // Check if the first column matches the key
        StringComparer comparer = StringComparer.OrdinalIgnoreCase;
        if (comparer.Compare(TickerSymbol, columns[0].Trim()) == 0)
        {
          // Return the second column value
          return columns[1].Trim();
        }
      }
      return String.Empty;
    }

    static public string GetCorpActionsFile(string TickerSymbol)
    {

      var stream = FileUtils.OpenFile(_mapFile);
      using var reader = new StreamReader(stream);

      // Read each line in the file
      while (!reader.EndOfStream)
      {
        var line = reader.ReadLine();

        if (line == null) continue;

        // Split line by comma, assuming only two columns
        var columns = line.Split(',');

        if (columns.Length < 3) continue; // Skip any malformed lines

        // Check if the first column matches the key
        StringComparer comparer = StringComparer.OrdinalIgnoreCase;
        if (comparer.Compare(TickerSymbol, columns[0].Trim()) == 0)
        {
          // Return the second column value
          return columns[2].Trim();
        }
      }
      return String.Empty;
    } 
  }
}
