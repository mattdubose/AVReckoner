using Reckoner.Utilities;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Reckoner.Models
{
    public enum AccountType 
    {
        TraditionalIRA,
        RothIRA, 
        HSA, 
        _529Plan,
        Taxable
    }
    public enum InvestmentStrategy
    {
        None,
        FWStrategy,
        BuyAndHold,
        MattStrategy
    }
    public class ActivityHolder 
    {
    public ActivityHolder() { }
    public ActivityHolder(Models.Action action, decimal amount, DateTime dateOfRequest) 
    {
      this.Action = action;
      this.Amount = amount;
      this.DateOfRequest = dateOfRequest;
    }
    public Action Action { get; set; }
    public decimal Amount  { get; set; } /* maybe I don't need this? A cash balance may be helpful, but whatever, just do this. */
    public DateTime DateOfRequest { get; set; }
  };
  public enum Action { Contribution, Distribution, Rebalance }
  public enum ActionInterval { Daily, Weekly, Monthly, Quarterly, Yearly, BiWeekly, SemiMonthly, SemiYearly }
  public enum InvestmentStage { Contributing, Distributing, Pause }

  public class Account
  {
    public decimal CashBalance { get; set; } = 0; // this is the cash balance in the account, not the cash balance of the assets.
    public int AccountId { get; set; }
    public int OwnerId { get; set; }
    public int ClientId { get; set; }
   
    [JsonConverter(typeof(JsonStringEnumConverter))] // Serialize/deserialize as a string
    public AccountType AccountType { get; set; }
    public List<ActivityHolder> StackedActivities { get; set; } 
    public List<SecurityHolding> Assets { get; set; } // this should be List<Asset> - but i'm still learnin'.
    public InvestmentSchedule? InvestmentSchedule { get; set; }
    public decimal InvestmentAmount { get; set; } = 0;
    public bool DividentReinvestment { get; set; } = true;

    [JsonConverter(typeof(JsonStringEnumConverter))] // Serialize/deserialize as a string
    public InvestmentStage? InvestmentStage { get; set; } = Models.InvestmentStage.Contributing;
    public InvestmentStrategy Strategy { get; internal set; } = InvestmentStrategy.None;
//    public string TickerToEvaluate { get; internal set; } = string.Empty; // this is the ticker symbol of the asset that is being evaluated for strategy purposes.

    public object StrategyConfiguration { get; internal set; } = null; // this is the settings for the strategy, e.g. FWStrategySettings, MattStrategySettings, etc.
    public decimal Dividends { get; internal set; }

    public Account() 
    { 
      Assets = new List<SecurityHolding>();
      StackedActivities = new List<ActivityHolder>();
    }
    public static Account DeepCopy(Account account)
    {
      /* deep copy constructor */
      Account newAccount = new Account();
      newAccount.AccountId = account.AccountId;
      newAccount.ClientId = account.ClientId;
      newAccount.AccountType = account.AccountType;
      newAccount.OwnerId = account.OwnerId;
      newAccount.InvestmentSchedule = new InvestmentSchedule(account.InvestmentSchedule);
      newAccount.Strategy = account.Strategy;
      newAccount.DividentReinvestment = account.DividentReinvestment;
      newAccount.Assets = new List<SecurityHolding>();
      foreach (var item in account.Assets)
      {
        SecurityHolding NewAsset = new SecurityHolding(item);
        newAccount.Assets.Add(NewAsset);
      }
      newAccount.StackedActivities = new List<ActivityHolder>();
      foreach (var item in account.StackedActivities)
      {
        ActivityHolder activityHolder = new ActivityHolder(item.Action,item.Amount,item.DateOfRequest);
        newAccount.StackedActivities.Add(activityHolder);
      }

      return newAccount; 
    }
  }
  public class InvestmentScheduleHelper
  {
    public static bool IsAdjustmentDay(InvestmentSchedule investmentSchedule, DateTime dateForInquiry) 
    {
      if ((investmentSchedule == null) || (investmentSchedule.AdjustmentFrequency == null))
      {
        return false;
      }
     return IsFrequencyMatch(investmentSchedule.AdjustmentFrequency, dateForInquiry,investmentSchedule.BeginContributions);
    }
    public static bool IsContributionDay(InvestmentSchedule investmentSchedule, DateTime dateForInquiry)
    {
      if ((investmentSchedule == null) || (investmentSchedule.ContributionFrequency == null))
      {
        return false;
      }
      return IsFrequencyMatch(investmentSchedule.ContributionFrequency, dateForInquiry, investmentSchedule.BeginContributions);
    }

    public static bool IsDistributionDay(InvestmentSchedule investmentSchedule, DateTime dateForInquiry)
    {
      if ((investmentSchedule == null) || (investmentSchedule.DistributionFrequency == null))
      {
        return false;
      }
      return IsFrequencyMatch(investmentSchedule.DistributionFrequency, dateForInquiry, investmentSchedule.BeginDistributions);
    }

    static private bool IsFrequencyMatch(ActionFrequency frequency, DateTime date, DateTime? referenceDate = null)
    {
      bool retVal = false;
      switch (frequency.Interval)
      {
        case ActionInterval.Daily:
          return true;

        case ActionInterval.Weekly:
          if (frequency.DayOfWeek.HasValue == false) { Debug.WriteLine("Weekly Frequency requires a day of week value to evaluate."); return false; }
          return (date.DayOfWeek == frequency.DayOfWeek.Value);

        case ActionInterval.BiWeekly:
          {

            if (frequency.DayOfWeek.HasValue == false) { Debug.WriteLine("BiWeekly Frequency requires a day of week value to evaluate."); return false; }

            // Assuming the reference start date is the first valid event date
            if (referenceDate == null) { Debug.WriteLine("BiWeekly Frequency requires a reference to evaluate."); return false; }
            DateTime refDate = (DateTime)referenceDate;
            return date.DayOfWeek == frequency.DayOfWeek.Value &&
                   (date - refDate).Days % 14 == 0;
          }

        case ActionInterval.Monthly:
        case ActionInterval.SemiMonthly:
          if (frequency.Dates == null) { Debug.WriteLine("Monthly/SemiMonthly Frequency requires the days list to be populated."); return false; }
          return frequency.Dates.Contains(date.Day);
        case ActionInterval.Quarterly:
          {
            if (referenceDate == null) { Debug.WriteLine("QuarterlyFrequency requires a reference to evaluate."); return false; }
            DateTime refDate = (DateTime)referenceDate;
            return date.DayOfYear % 90 == 0;
          }
        case ActionInterval.SemiYearly:
          {
            if (referenceDate == null) { Debug.WriteLine("Annual/semiAnnual Frequency requires a reference to evaluate."); return false; }
            DateTime refDate = (DateTime)referenceDate;
            return date.DayOfYear % 180 == 0;
          }
        case ActionInterval.Yearly:
          {
            if (referenceDate == null) { Debug.WriteLine("Annual/semiAnnual Frequency requires a reference to evaluate."); return false; }
            DateTime refDate = (DateTime)referenceDate;
            return date.DayOfYear == refDate.DayOfYear;
          }
      }

      return false;
    }

  }
  public class InvestmentSchedule
  {

    public InvestmentSchedule() { }
    public InvestmentSchedule(DateTime beginCont, DateTime endCont, DateTime beginDist)
    {
      BeginContributions = beginCont;
      EndContributions = endCont;
      BeginDistributions = beginDist;
    }
    public InvestmentSchedule(InvestmentSchedule? toCopy) 
    {
      AdjustmentFrequency = toCopy?.AdjustmentFrequency!= null ? new ActionFrequency(toCopy.AdjustmentFrequency) : null; 
      ContributionFrequency = toCopy?.ContributionFrequency != null ? new ActionFrequency(toCopy.ContributionFrequency) : null;
      DistributionFrequency = toCopy?.DistributionFrequency!= null ? new ActionFrequency(toCopy.DistributionFrequency) : null;
      EndContributions   = toCopy?.EndContributions != null ? toCopy.EndContributions : null;
      BeginContributions = toCopy?.BeginContributions != null ? toCopy.BeginContributions : null;
      BeginDistributions = toCopy?.BeginDistributions != null ? toCopy.BeginDistributions : null;
    }
    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? BeginContributions { get; set; }
    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? EndContributions { get; set; }
    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateTime? BeginDistributions { get; set; }
    public ActionFrequency? ContributionFrequency { get; set; }
    public ActionFrequency? DistributionFrequency { get; set; }
    public ActionFrequency? AdjustmentFrequency { get; set; }

    internal void AdjustDates()
    {
      if (BeginContributions.HasValue && ContributionFrequency != null) 
      {
        if (ContributionFrequency.DayOfWeek.HasValue)
        {
          /* adjust the beginContributions until it's the day of the week to make the contribution.*/
          while (BeginContributions.Value.DayOfWeek != ContributionFrequency.DayOfWeek.Value) 
          {
            BeginContributions = BeginContributions.Value.AddDays(1);
          }
        }
      }
      if (BeginDistributions.HasValue && DistributionFrequency != null)
      {
        if (DistributionFrequency.DayOfWeek.HasValue)
        {
          /* adjust the beginContributions until it's the day of the week to make the contribution.*/
          while (BeginDistributions.Value.DayOfWeek != DistributionFrequency.DayOfWeek.Value)
          {
            BeginDistributions = BeginDistributions.Value.AddDays(1);
          }
        }
      }

    }
  }

  public class ActionFrequency
  {
    public ActionFrequency(ActionFrequency old)
    {
        Interval = old.Interval;
        DayOfWeek = old.DayOfWeek;
        Dates = new List<int>(old.Dates);
    }
    public ActionFrequency() { }
    public List<int> Dates { get; set; } = new List<int>();/* e.g 1st & 15th of each month. */ 
    [JsonConverter(typeof(JsonStringEnumConverter))] // Serialize/deserialize as a string
    public ActionInterval Interval { get; set; } = ActionInterval.Weekly;
    [JsonConverter(typeof(JsonStringEnumConverter))] // Serialize/deserialize as a string
    public DayOfWeek? DayOfWeek { get; set; }


    public ActionFrequency(ActionInterval interval, List<int> dates, DayOfWeek? dayOfWeek)
    {
        Dates = dates;
        Interval = interval;
        DayOfWeek = dayOfWeek;
    }
  }
}
