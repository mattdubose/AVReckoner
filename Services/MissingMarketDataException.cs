namespace Reckoner.Services
{
    public class MissingMarketDataException : Exception
    {
        public IReadOnlyList<string> Tickers { get; }

        public MissingMarketDataException(IReadOnlyList<string> tickers)
            : base($"No price data loaded for: {string.Join(", ", tickers)}. Load it in Market Data Admin first.")
        {
            Tickers = tickers;
        }
    } 
}
