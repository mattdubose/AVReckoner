using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reckoner.Utilities
{
    public interface IDateProvider
    {
      public DateTime Now { get; }
      DateTime GetCurrentDate();
      void SetCurrentDate(DateTime date);
    }
    public class RealTimer : IDateProvider
    {
      public DateTime Now => DateTime.Now;

      DateTime IDateProvider.GetCurrentDate() => DateTime.Now.Date;
      void IDateProvider.SetCurrentDate(DateTime date)
      {
        throw new NotImplementedException();
      }
    }
    public class ManagedDateTime : IDateProvider
    {
      DateTime _dateTime = DateTime.Now;
      public DateTime Now => _dateTime;

      public void SetCurrentDate(DateTime date)
      {
        _dateTime = date;
      }
      public DateTime GetCurrentDate()
      {
        return _dateTime;
      }
      public void AddDays(int NumDays) 
      {
        _dateTime = _dateTime.AddDays(NumDays);
      }
    }
}
