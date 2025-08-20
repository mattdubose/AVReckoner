using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Utilities
{
  public class ChangeTracker
  {
    private readonly HashSet<string> trackedProperties = new();
    private bool isModified;

    public bool IsModified => isModified;

    public void TrackProperty(INotifyPropertyChanged source, string propertyName)
    {
      trackedProperties.Add(propertyName);
      source.PropertyChanged += (sender, args) =>
      {
        if (trackedProperties.Contains(args.PropertyName))
        {
          isModified = true;
        }
      };
    }
    public void Reset () { isModified =  false; }


    /* Usage  example. 
     * public partial class InvestmentPerformanceViewModel : ObservableObject
{
    private readonly ChangeTracker changeTracker = new();

    [ObservableProperty]
    private DateTime? firstContributionDate;

    [ObservableProperty]
    private decimal? contributionAmount;

    [ObservableProperty]
    private DateTime? lastContributionDate; // Not tracked

    public bool IsModified => changeTracker.IsModified;

    public InvestmentPerformanceViewModel()
    {
        // Track specific properties
        changeTracker.TrackProperty(this, nameof(FirstContributionDate));
        changeTracker.TrackProperty(this, nameof(ContributionAmount));
    }
}
*/
  }
}

