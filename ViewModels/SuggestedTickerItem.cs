namespace Reckoner.ViewModels
{
    public partial class SuggestedTickerItem : ObservableObject
    {
        public string Ticker { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;

        [ObservableProperty] bool isSelected;
        [ObservableProperty] bool isAlreadyLoaded;
    }
}
