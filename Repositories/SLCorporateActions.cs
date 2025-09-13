using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;

namespace Reckoner.Repositories
{
    public class SqliteCorporateActions : ICorporateActionProvider
    {
        private readonly string _connectionString;
        private readonly string _ticker;

        public SqliteCorporateActions(string connectionString, string ticker)
        {
            _connectionString = connectionString;
            _ticker = ticker;
        }

        private IDbConnection Connection => new SqliteConnection(_connectionString);

        public DailyEquityInfo? GetInfo(DateTime date)
        {
            using var conn = Connection;
            return conn.QueryFirstOrDefault<DailyEquityInfo>(
                @"SELECT date, open, high, low, close, NULL as overallhigh
                  FROM price_history 
                  WHERE ticker = @ticker AND date = @date",
                new { ticker = _ticker, date });
        }

        public List<DailyEquityInfo> GetInfoBetweenDates(DateTime startDate, DateTime endDate)
        {
            using var conn = Connection;
            return conn.Query<DailyEquityInfo>(
                @"SELECT date, open, high, low, close, NULL as overallhigh
                  FROM price_history 
                  WHERE ticker = @ticker AND date BETWEEN @start AND @end 
                  ORDER BY date",
                new { ticker = _ticker, start = startDate, end = endDate }).AsList();
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
                new { ticker = _ticker, endDate, limit = numberToGet }).AsList();
        }

        public DailyEquityInfo? GetLatestDaysInfo(DateTime startDate, int maxLookback)
        {
            var data = GetLastXDays(startDate, maxLookback);
            return data.FirstOrDefault();
        }

        public List<CorporateAction> GetActions(DateTime date)
        {
            using var conn = Connection;
            return conn.Query<CorporateAction>(
                @"SELECT 
                      id AS AccountId,
                      ticker AS Ticker, 
                      action_type AS ActionType, 
                      action_value AS ActionValue, 
                      effective_date AS EffectiveDate
                  FROM corporate_actions
                  WHERE effective_date = @date AND ticker = @ticker
                  ORDER BY action_type",
                new { date, _ticker }).AsList();
        }

        public List<CorporateAction> GetDividends(DateTime startDate, DateTime endDate)
        {
            using var conn = Connection;
            return conn.Query<CorporateAction>(
                @"SELECT 
                      id AS AccountId,
                      ticker AS Ticker, 
                      action_type AS ActionType, 
                      action_value AS ActionValue, 
                      effective_date AS EffectiveDate
                  FROM corporate_actions
                  WHERE (effective_date BETWEEN @startDate AND @endDate) AND ticker = @ticker
                  ORDER BY action_type",
                new { startDate, endDate, _ticker }).AsList();
        }

        public List<CorporateAction> GetDividend(DateTime date)
        {
            using var conn = Connection;
            return conn.Query<CorporateAction>(
                @"SELECT 
                      ticker, 
                      action_type AS ActionType, 
                      action_value AS ActionValue, 
                      effective_date AS EffectiveDate
                  FROM corporate_actions
                  WHERE effective_date = @date AND ticker = @ticker AND action_type = 'dividend'",
                new { date, _ticker }).AsList();
        }

        public List<CorporateAction> GetSplits(DateTime date)
        {
            using var conn = Connection;
            return conn.Query<CorporateAction>(
                @"SELECT 
                      ticker, 
                      action_type AS ActionType, 
                      action_value AS ActionValue, 
                      effective_date AS EffectiveDate
                  FROM corporate_actions
                  WHERE effective_date = @date AND ticker = @ticker AND action_type = 'split'",
                new { date, _ticker }).AsList();
        }

        public void InsertIfNotExists(CorporateAction action)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Execute(
                @"INSERT OR IGNORE INTO corporate_actions 
          (ticker, action_type, action_value, effective_date)
          VALUES (@Ticker, @ActionType, @ActionValue, @EffectiveDate)",
                action);
        }

    }
}
