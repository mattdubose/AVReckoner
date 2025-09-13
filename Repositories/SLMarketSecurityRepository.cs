using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using Dapper;
using Reckoner.Models;

namespace Reckoner.Repositories
{
    public class SqliteMarketSecurityRepository : IMarketSecurityRepository
    {
        private readonly string _connectionString;

        public SqliteMarketSecurityRepository(string connectionString)
        {
            _connectionString = $"Data Source={connectionString}";
        }

        public List<MarketSecurity> GetAll()
        {
            using var conn = new SqliteConnection(_connectionString);
            string query = "SELECT ticker AS TickerSymbol, name AS Name FROM market_securities ORDER BY ticker";
            return conn.Query<MarketSecurity>(query).AsList();
        }

        public List<MarketSecurity> SearchByTicker(string input)
        {
            using var conn = new SqliteConnection(_connectionString);
            string query = @"
                SELECT ticker AS TickerSymbol, name AS Name 
                FROM market_securities 
                WHERE is_active = 1 AND 
                      LOWER(ticker) LIKE LOWER(@input)
                ORDER BY ticker";
            return conn.Query<MarketSecurity>(query, new { input = $"%{input}%" }).AsList();
        }

        public List<MarketSecurity> SearchByTickerOrName(string input)
        {
            using var conn = new SqliteConnection(_connectionString);
            string query = @"
                SELECT ticker AS TickerSymbol, name AS Name 
                FROM market_securities 
                WHERE is_active = 1 AND 
                      (LOWER(ticker) LIKE LOWER(@input) OR LOWER(name) LIKE LOWER(@input))
                ORDER BY ticker";
            return conn.Query<MarketSecurity>(query, new { input = $"%{input}%" }).AsList();
        }

        public void Upsert(MarketSecurityRecord record)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Execute(@"
        INSERT INTO market_securities 
        (ticker, name, earliest_date, latest_date, modified_date)
        VALUES (@TickerSymbol, @Name, @EarliestDate, @LatestDate, @ModifiedDate)
        ON CONFLICT(ticker) DO UPDATE SET
            name = excluded.name,
            earliest_date = excluded.earliest_date,
            latest_date = excluded.latest_date,
            modified_date = excluded.modified_date",
                record);
        }

    }
}
