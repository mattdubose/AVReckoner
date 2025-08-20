using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reckoner.Utilities;
namespace Reckoner.Services
{
  public sealed class DateTimeService 
  {
    IDateProvider dateProvider;
    private static readonly DateTimeService _instance = new DateTimeService();
    // Public static property to provide global access to the instance
    public static DateTimeService GetInstance => _instance;

    private DateTimeService() 
    {
      dateProvider = new RealTimer();
    }
    public void SetDateProvider(IDateProvider dateProvider)
    {
      this.dateProvider = dateProvider;
    }
    public DateTime GetCurrentDate() 
    {
      return dateProvider.GetCurrentDate();
    }
  }
}
