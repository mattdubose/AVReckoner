using System;
using System.Collections.Generic;
using System.IO;

namespace Reckoner.Utilities;
public class CsvReader
{
  public static List<int> GetListOfInts(string inputString) 
  {
    List<int> list = new List<int>();
    var values = inputString.Split(',');
    foreach (var v in values)
    {
      int result;
      bool didParse = int.TryParse(v,out result);
      if (didParse)
      {
        list.Add(result);
      }
    }
    return list;
  }
  public static string? GetValueByKey(string filePath, string keyToFind)
  {
    // Open the file for reading
    using var reader = new StreamReader(filePath);

    // Read each line in the file
    while (!reader.EndOfStream)
    {
      var line = reader.ReadLine();

      if (line == null) continue;

      // Split line by comma, assuming only two columns
      var columns = line.Split(',');

      if (columns.Length < 2) continue; // Skip any malformed lines

      // Check if the first column matches the key
      if (columns[0].Trim() == keyToFind)
      {
        // Return the second column value
        return columns[1].Trim();
      }
    }

    // Return null if the key wasn't found
    return null;
  }
}
