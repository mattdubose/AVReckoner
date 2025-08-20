using Reckoner.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reckoner.Utilities;
using Reckoner.Models;

namespace Reckoner.Services
{
  public class TimeRunner
  {
    public TimeRunner(IAccountRepository accountsRepo) { }

    public void RunUntilDate(DateTime endDate)
    { 

    }
    public void RunUntilDate (AccountService accountService, DateTime endDate) 
    {
      Account account = accountService.GetAccount();
      ManagedDateTime managedTime = new ManagedDateTime();
      if (account.InvestmentSchedule == null)
      {
        Debug.WriteLine("Run Until Date has no investment schedule - nothing to do.");
        return;
      }
      
      DateTime? startDate = account.InvestmentSchedule.BeginContributions;
      if (!startDate.HasValue) 
      {
        startDate = endDate.AddDays(-90);/* go back 90 days. */
      }
      managedTime.SetCurrentDate((DateTime)startDate);
      DateTimeService.GetInstance.SetDateProvider(managedTime);
      while (managedTime.GetCurrentDate() > endDate)
      {
        accountService.RunDaysActivities();
      }
      Debug.WriteLine($"Final balance is {accountService.GetBalance()}");
    }
    public void RunTheDay()
    {
      /* get all the clients */
      /* each client populates it's list of accounts. */
      /* each account populates it's holdings. */
      /* each holding points to it's dailyEquityInfo */
      /* for each account determine if there is an activity (contribution/distrubution) or if it's due for a rebalance. */
      /* chart/print the balance at the end of the day for AUM or PerCustomer/Account . (decimation if needed.) */
    }

  }
}
