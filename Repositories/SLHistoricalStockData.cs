using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;
using Reckoner.Services;

namespace Reckoner.Repositories
{
    public class SLMarketSecurityHelper //  Sqlite 
    {
        public static List<AssetService> BuildAssetServices(Account account)
        {
            List<AssetService> assetServices = new List<AssetService>();
            var connString = "ReckonerDB.db";

            foreach (var asset in account.Assets)
            {
                try
                {
                    SqliteHistoricalStockData stockData = new SqliteHistoricalStockData(connString, asset.ID);
                    CachingHistoricalStockData cachingHistoricalStockData = new CachingHistoricalStockData(stockData);
                    HistoricalBasedMarketInterface _historicalInterface = new HistoricalBasedMarketInterface(cachingHistoricalStockData);

                    SqliteCorporateActions corpActions = new SqliteCorporateActions(connString, asset.ID);
                    CachingCorporateActionData cachingCorpActions = new CachingCorporateActionData(corpActions);
                    AssetService newservice = new AssetService(_historicalInterface, cachingCorpActions, asset);
                    //  string corpFile = MarketSecuritiesMap.GetCorpActionsFile(asset.ID);
                    //  var corpStream = FileUtils.OpenFile(corpFile);
                    //  //newservice.SetDividends(BarChartsCorporateActions.GetListOfDividends(corpStream));
                    assetServices.Add(newservice);
                }
                catch (Exception ex)
                {
                    Debug.Write(ex);
                }
            }
            return assetServices;
        }
    }

    public class SqliteHistoricalStockData : IHistoricalStockData
    {
        private readonly string _connectionString;
        private MarketInterfaceErrors _lastError = MarketInterfaceErrors.NoError;
        private readonly string _ticker;

        public SqliteHistoricalStockData(string connectionString, string ticker)
        {
            _connectionString = $"Data Source={connectionString}";
            _ticker = ticker;
        }

        private IDbConnection Connection => new SqliteConnection(_connectionString);

        public DailyEquityInfo? GetInfo(DateTime date)
        {
            using var conn = Connection;
            var result = conn.QueryFirstOrDefault<DailyEquityInfo>(
                @"SELECT date, open, high, low, close, NULL as overallhigh
                  FROM price_history 
                  WHERE ticker = @ticker AND date = @date",
                new { ticker = _ticker, date });

            if (result == null)
                _lastError = MarketInterfaceErrors.DateNotPresent;

            return result;
        }

        public List<DailyEquityInfo> GetInfoBetweenDates(DateTime startDate, DateTime endDate)
        {
            using var conn = Connection;
            return conn.Query<DailyEquityInfo>(
                @"SELECT date, open, high, low, close, NULL as overallhigh
                  FROM price_history 
                  WHERE ticker = @ticker AND date BETWEEN @start AND @end 
                  ORDER BY date",
                new { ticker = _ticker, start = startDate, end = endDate }).ToList();
        }

        public List<DailyEquityInfo> GetLastXDays(DateTime endDate, int numberToGet)
        {
            using var conn = Connection;
            return conn.Query<DailyEquityInfo>(
                @"SELECT date, open, high, low, close, NULL as overallhigh
                  FROM price_history 
                  WHERE ticker = @ticker AND date <= @endDate 
                  ORDER BY date DESC 
                  LIMIT @limit",
                new { ticker = _ticker, endDate, limit = numberToGet }).ToList();
        }

        public DailyEquityInfo? GetLatestDaysInfo(DateTime startDate, int maxLookback)
        {
            var data = GetLastXDays(startDate, maxLookback);
            return data.FirstOrDefault();
        }

        public void InsertIfNotExists(DailyEquityInfo info)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Execute(
                @"INSERT OR IGNORE INTO price_history 
          (ticker, date, open, high, low, close, volume) 
          VALUES (@ticker, @date, @open, @high, @low, @close, @volume)",
                new
                {
                    ticker = _ticker,
                    date = info.Date,
                    open = info.Open,
                    high = info.High,
                    low = info.Low,
                    close = info.Close,
                    volume = info.Volume
                });
        }



        public MarketInterfaceErrors GetLastError() => _lastError;
        public void ClearErrors() => _lastError = MarketInterfaceErrors.NoError;
    }
}
