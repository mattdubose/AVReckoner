using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Reckoner.Utilities;
using System.IO;

namespace Reckoner.Repositories
{
  public class BarChartsHelper 
  {
    public static List<AssetService> BuildAssetServices(Account account)
    {
      List<AssetService> assetServices = new List<AssetService>();

      foreach (var asset in account.Assets)
      {
        try
        {
          string result = MarketSecuritiesMap.GetSecuritiesFile(asset.ID);
          if (result != null && result.Length > 0)
          {
            var stream = FileUtils.OpenFile(result);

            BarChartsHistoryRepo stockData = new BarChartsHistoryRepo(stream);
            HistoricalBasedMarketInterface _historicalInterface = new HistoricalBasedMarketInterface(stockData);
            AssetService newservice = new AssetService(_historicalInterface, null, asset);
          //  string corpFile = MarketSecuritiesMap.GetCorpActionsFile(asset.ID);
          //  var corpStream = FileUtils.OpenFile(corpFile);
          //  //newservice.SetDividends(BarChartsCorporateActions.GetListOfDividends(corpStream));
            assetServices.Add(newservice);
          }
          else 
          {
            Debug.WriteLine($"GetSecuritiesFile returned null or empy string for ID: {asset.ID}!");
          }
        }
        catch (Exception ex) 
        {
          Debug.Write(ex);
        }
      }
      return assetServices;
    }
  }
  public class BarChartsHistoryRepo : IHistoricalStockData, IDisposable
  {
    Stream _stream;
    private bool _disposed = false;
    MarketInterfaceErrors _error;
    private DailyEquityInfo? lastFound;
    private bool _isAscendingOrder = true;

    /* TODO !!! FIRST THING FIRSTTHING  move DetermineOrder to a utility. and then use the _isAscendingOrder flag in implementaiton. */
    private void DetermineOrder() 
    {
      int lineCount = 0;
      ResetStream();
      StreamReader reader = new StreamReader(_stream);
      DateTime? firstDate = null;
      while (!reader.EndOfStream)
      {
        var line = reader.ReadLine();
        lineCount++;
        if (line == null)
        {
          if (lineCount < 5)
            continue;
          else
            break;
        }

        DateTime lineDate;
        var columns = line.Split(',');
        if (columns.Length == 0) continue;
        if (TryParseDate(columns[0], out lineDate))
        {
          if (firstDate == null)
          {
            firstDate = lineDate;
            continue;
          }
          else
          {
            if (firstDate > lineDate)
            {
              _isAscendingOrder = false;
              return;
            }
            else if (firstDate > lineDate)
            {
              _isAscendingOrder = true;
              return;
            }
          }
        }
        else
        {
          if (lineCount > 5) break;
        }
      }
    }
    public void Dispose()
    {
      if (!_disposed)
      {
        _stream?.Dispose();
        _disposed = true;
      }
    }
    public BarChartsHistoryRepo(Stream stream) 
    {
      _stream = stream;
      DetermineOrder();
    }
    public void ResetStream()
    {
      if (_stream == null)
        throw new ObjectDisposedException(nameof(_stream));
      _stream.Position = 0; // Reset stream to the beginning
    }

    static public bool TryParseDate(string dateString, out DateTime result)
    {
      if (DateTime.TryParseExact(dateString, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture,
                                 System.Globalization.DateTimeStyles.None, out result))
      {
        return true;
      }
      else return (DateTime.TryParseExact(dateString, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                                 System.Globalization.DateTimeStyles.None, out result));

    }
    private DailyEquityInfo? getDailyEquityInfoFromLine(string[] line)
    { /* Time,Open,High,Low,Last,Change,%Chg,Volume */
      DailyEquityInfo dailyEquityInfo = new DailyEquityInfo();
      try
      {
        DateTime date;
        if (TryParseDate(line[0], out date))
        {
          dailyEquityInfo.Date = date;
        } // = DateTime.ParseExact(line[0], "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);
        else
        { throw new KeyNotFoundException(); }

        dailyEquityInfo.Open = decimal.Parse(line[1]);
        dailyEquityInfo.High = decimal.Parse(line[2]);
        dailyEquityInfo.Low = decimal.Parse(line[3]);
        dailyEquityInfo.Close = decimal.Parse(line[4]);
      }
      catch (Exception ex)
      {
        Debug.WriteLine(ex.Message);
        Debug.WriteLine("COULD NOT PARSE Daily EquityInfo");
        _error = MarketInterfaceErrors.ParsingError;
        return null;
      }
      lastFound = dailyEquityInfo;
      return dailyEquityInfo;

    }
    public DailyEquityInfo? GetInfo(DateTime dateOfInterest)
    {
      if (lastFound != null)
      {
        if (lastFound.Date == dateOfInterest) 
        {
          return lastFound;
        }
      }
      if (_isAscendingOrder)
      {
        return GetAscendingInfo(dateOfInterest);
      }
      string matchDateSlash = dateOfInterest.ToString("MM/dd/yyyy");
      string matchDateDash = dateOfInterest.ToString("yyyy-MM-dd");
      ResetStream();
      StreamReader reader = new StreamReader(_stream);
      int count = 0;
      _error = MarketInterfaceErrors.NoError;
      while (!reader.EndOfStream)
      {
        var line = reader.ReadLine();

        if (line == null) continue;

        DateTime lineDate;
        var columns = line.Split(',');
        if (columns.Length == 0) continue;
        if ((columns[0] == matchDateSlash) || (columns[0] == matchDateDash))
        {
          _error = MarketInterfaceErrors.NoError;
          return getDailyEquityInfoFromLine(columns);

        }
        else if (TryParseDate(columns[0], out lineDate))
        {
          if (lineDate == dateOfInterest)
          {
            _error = MarketInterfaceErrors.NoError;
            return getDailyEquityInfoFromLine(columns);
          }
          if (lineDate < dateOfInterest)
          {/* date isn't going to be present -- this is particular to the order date - where data is in Descending order. */
            if (count == 0)
            {
              _error = MarketInterfaceErrors.InvalidDate;
            }
            else 
              _error = MarketInterfaceErrors.DateNotPresent;
            return null;
          }

        }
        else
        {
          _error = MarketInterfaceErrors.ParsingError;
        }
        count++;
      }
      if (_error == MarketInterfaceErrors.NoError)
        _error = MarketInterfaceErrors.InvalidDate;
      return null;
    }
    DateTime lastDate = new DateTime(3000,1,1);
    long lastPosition = 0;
    public DailyEquityInfo? GetAscendingInfo(DateTime dateOfInterest)
    {
      if (lastFound != null)
      {
        if (lastFound.Date == dateOfInterest)
        {
          return lastFound;
        }
      }
      bool setPositon = true;
      //      if (lastDate > dateOfInterest)
      //      {
      //        ResetStream();
      //        setPositon = false;
      //      }
      ResetStream();
      StreamReader reader = new StreamReader(_stream);
//      if (setPositon)
//      {
//        reader.BaseStream.Seek(lastPosition, SeekOrigin.Begin);
//        reader.DiscardBufferedData();
//      }
//      lastDate = dateOfInterest;

      string matchDateSlash = dateOfInterest.ToString("MM/dd/yyyy");
      string matchDateDash = dateOfInterest.ToString("yyyy-MM-dd");
      int count = 0;
      _error = MarketInterfaceErrors.NoError;
      while (!reader.EndOfStream)
      {
        var line = reader.ReadLine();

        if (line == null) continue;

        DateTime lineDate;
        var columns = line.Split(',');
        if (columns.Length == 0) continue;
        if ((columns[0] == matchDateSlash) || (columns[0] == matchDateDash))
        {
          _error = MarketInterfaceErrors.NoError;
          return getDailyEquityInfoFromLine(columns);

        }
        else if (TryParseDate(columns[0], out lineDate))
        {
          if (lineDate == dateOfInterest)
          {
            _error = MarketInterfaceErrors.NoError;
            return getDailyEquityInfoFromLine(columns);
          }
          if (lineDate > dateOfInterest)
          {/* date isn't going to be present -- this is particular to the order date - where data is in Descending order. */
            if (count == 0)
            {
              _error = MarketInterfaceErrors.InvalidDate;
            }
            else
              _error = MarketInterfaceErrors.DateNotPresent;
            return null;
          }

        }
        else
        {
          _error = MarketInterfaceErrors.ParsingError;
        }
        count++;
      }
      if (_error == MarketInterfaceErrors.NoError)
        _error = MarketInterfaceErrors.InvalidDate;
      return null;
    }

    public List<DailyEquityInfo> GetInfoBetweenDates(DateTime startDate, DateTime endDate)
    {
      /* So, originally, I just called GetInfo() with the date over and over from start to End, 
       * but this is inefficient, as I i reset the streamreader in the getInfo, so I'll do it here, and read most effectively.*/
      
      List<DailyEquityInfo> dailyEquityInfos = new List<DailyEquityInfo>();
      if (endDate < startDate)
      {
        //let's just reverse it for them. 
        DateTime tmp = startDate;
        startDate = endDate;
        endDate = tmp;
      }
      DateTime dateForQuery = startDate;

      ResetStream();
      StreamReader reader = new StreamReader(_stream);

      while ((dateForQuery < endDate) && !reader.EndOfStream)
      {
        var line = reader.ReadLine();
        if (line == null) continue;
        var columns = line.Split(',');
        if (columns.Length == 0) continue;
        DateTime lineDate;
        if (TryParseDate(columns[0], out lineDate))
        {
          if (lineDate < endDate && lineDate > startDate)
          {
            DailyEquityInfo? newDay = getDailyEquityInfoFromLine(columns);
            if (newDay != null)
            {
              dailyEquityInfos.Add(newDay);
            }
          }
          else if ((lineDate < startDate) && (!_isAscendingOrder))
          {
            return dailyEquityInfos;
          }
          else if ((_isAscendingOrder) && (lineDate > startDate)) 
          {
            return dailyEquityInfos;
          }
        }
        else
        {
          _error = MarketInterfaceErrors.ParsingError;
        }
      }
      return dailyEquityInfos;
    }

    public List<DailyEquityInfo> GetLastXDays(DateTime endDate, int NumberToGet)
    {
      DateTime startDate;
      if (NumberToGet > 0)
      {
        NumberToGet *= -1;
      }
      startDate = endDate.AddDays(NumberToGet) ;
      return GetInfoBetweenDates(startDate, endDate);
    }

    public DailyEquityInfo? GetLatestDaysInfo(DateTime startDate, int MaxLookback)
    {
      DailyEquityInfo? dailyEquityInfo = null;
      for (int i = 0; i < MaxLookback + 1; i++)
      {
        try 
        {
          dailyEquityInfo = GetInfo(startDate.AddDays(-i));
          if (dailyEquityInfo != null)
          {
            return dailyEquityInfo;
          }
        }
        catch 
        {
          return null;
        }
      }
      return dailyEquityInfo;
    }

    public MarketInterfaceErrors GetLastError()
    {
      return _error;
    }

    public void ClearErrors()
    {
      _error = MarketInterfaceErrors.NoError;
    }
  }
  public class BarChartsCorporateActions
  {
    Dictionary<DateTime, decimal> GetListOfSplits(Stream stream) 
    {
      /* I DON"T KNOW IF I NEED THIS!!!! */
      Dictionary<DateTime, decimal> results = new Dictionary<DateTime, decimal>();
      StreamReader reader = new StreamReader(stream);
      while (!reader.EndOfStream)
      {
        var line = reader.ReadLine();

        if (line == null) continue;

        DateTime lineDate;
        var columns = line.Split(',');
        if (columns.Length < 3) continue;
        if (columns[1].Contains("Split"))
        {
          if (BarChartsHistoryRepo.TryParseDate(columns[0], out lineDate))
          {
            decimal dividendAmount = 0;
            if (decimal.TryParse(columns[2], out dividendAmount))
            {
              results[lineDate] = dividendAmount;
            }

          }
        }
      }
      return results;
    }

      
    static public Dictionary<DateTime, decimal> GetListOfDividends(Stream stream)
    {
      Dictionary<DateTime, decimal> results = new Dictionary<DateTime, decimal>();
      StreamReader reader = new StreamReader(stream);
      while (!reader.EndOfStream)
      {
        var line = reader.ReadLine();

        if (line == null) continue;

        DateTime lineDate;
        var columns = line.Split(',');
        if (columns.Length < 3) continue;
        if (columns[1].Contains("Dividend"))
        {
          if (BarChartsHistoryRepo.TryParseDate(columns[0], out lineDate))
          {
            decimal dividendAmount = 0;
            if (decimal.TryParse(columns[2], out dividendAmount))
            {
              results[lineDate] = dividendAmount;
            }

          }
        }
      }
      return results;
    }
  }
}
