using Reckoner.Repositories;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Reckoner.Services
{

  public class AccountService
  {
    private SuggestedAction _latestAction;
    private Account _account;
    public List<AssetService> Assets;
    public FWInvestmentStrategy InvestmentStrategyService;
    public Account GetAccount() { return _account; }
    public AccountService(Account myAccount, List<AssetService> RTAssets) 
    {
        _account = myAccount;
        Assets = RTAssets;
        InvestmentStrategyService = new FWInvestmentStrategy (Assets, _account.StrategyConfiguration);
    }
    public decimal AllTimeHigh { get; private set; } = 0;
    bool CanPerformTradeActionToday()
    {
        foreach (AssetService asset in Assets)
        {
            if (asset.GetCurrentPrice() <= 0) return false;
        }
        return true;
    }
    public void Contribute(decimal amountInDollars)
    {
      decimal dollarsContributed = 0;
      foreach (AssetService asset in Assets)
      {
        decimal dollarsForThisAsset = 0;
        if (asset.ContributionPercentage != null)
        {
          dollarsForThisAsset = (decimal)(asset.ContributionPercentage * amountInDollars);
        }
        asset.BuyInDollars(dollarsForThisAsset);
        dollarsContributed += dollarsForThisAsset;
      }
      if (dollarsContributed != amountInDollars)
      {
        Debug.WriteLine($"Ammount contributed {amountInDollars}did not equal amount spent:{dollarsContributed}");
      }
    }
    public void Distribute(decimal amountInDollars)
    {
      /* remove in the opposite way you would distribute. */
      decimal dollarsDistributed = 0;
      foreach (AssetService asset in Assets)
      {
        decimal dollarsFromThisAsset = 0;
        if (asset.DistributionPercentage != null)
        {
          dollarsFromThisAsset = (decimal)(asset.DistributionPercentage * amountInDollars);
        }
        else 
        {
          Debug.WriteLine($"Distribution for {asset.TickerSymbol} is null!");
        }
        asset.SellInDollars(dollarsFromThisAsset);
        dollarsDistributed += dollarsFromThisAsset;
      }
    }
    public decimal GetBalance() 
    {
      decimal balance = 0;
      foreach (AssetService asset in Assets)
      {
        decimal price = asset.GetLatestPrice();
        balance += asset.NumberOfShares * price;
      }
      if (balance > AllTimeHigh) 
      {
        AllTimeHigh = balance;
      }
      return balance;

    }
    public void Rebalance()
    { /* TODO this is fine for now, but in realife, this would be buys and sells. */
      decimal totalBalance = GetBalance();
      if (totalBalance <= 0)
      {
        Debug.WriteLine("Attepting to rebalance when balance is 0.  Leaving.");
        return;
      }  
      foreach (AssetService asset in Assets)
      {
        if (asset.ContributionPercentage != null)
        {
          decimal price = asset.GetCurrentPrice();
          decimal holdingBalance = price * asset.NumberOfShares;
          decimal actualPercentage = holdingBalance / totalBalance;
          decimal changeInPercentage = (decimal)asset.ContributionPercentage - actualPercentage;
          decimal changeInValue = changeInPercentage * totalBalance;
          decimal changeInShares = changeInValue / price;
          asset.NumberOfShares += changeInShares;
        }
      }
      decimal newBalance = GetBalance();
      if (Math.Round(newBalance,4) != Math.Round(totalBalance,4)) 
      {
        Debug.WriteLine("Error when rebalancing! Beginning and Ending don't match.");
      }

    }

    public void RunDaysActivities()
    {
            //      Debug.WriteLine($"Running activities for {_account.AccountId} on {DateTimeService.GetInstance.GetCurrentDate()}");

        DateTime today = DateTimeService.GetInstance.GetCurrentDate();
        HandleCorporateActions(today);
        //wmdTODO      ExecuteEvaluation();
        if (_account.Strategy == Models.InvestmentStrategy.FWStrategy) 
        {
            HandleInvestmentStrategy();
        }
        HandleStackedActivities();
        if (_account == null) 
        {
          Debug.WriteLine($"RunDaysActivities can't do anything - no account {_account.AccountId}");
          throw new InvalidOperationException();
          return;
        }
        if (_account.InvestmentSchedule == null)
        {
          Debug.WriteLine($"RunDaysActivities can't do anything - no investment schedule for account {_account.AccountId}");
          throw new InvalidOperationException();
          return;
        }

        if (_account.InvestmentStage == InvestmentStage.Contributing)
        {
          if (InvestmentScheduleHelper.IsContributionDay(_account.InvestmentSchedule, today))
          {
            Debug.WriteLine($"{today.ToString("MM-dd-yyyy")} is a contribution day");
            if (CanPerformTradeActionToday() == false)
            {
              _account.StackedActivities.Add(new ActivityHolder (Models.Action.Contribution, _account.InvestmentAmount, today));
              Debug.WriteLine("Can't perform buy action today, will delay.");
            }
            else
            {
              Contribute(_account.InvestmentAmount);
              Debug.WriteLine($"Balance is: {GetBalance()} after contribution.");
            }
          }
        }
        if (InvestmentScheduleHelper.IsAdjustmentDay(_account.InvestmentSchedule, today))
        {
          Debug.WriteLine($"{today.ToString("MM-dd-yyyy")} is an adjustement day");
          if (CanPerformTradeActionToday() == false)
          {
            _account.StackedActivities.Add(new ActivityHolder(Models.Action.Rebalance,0, today));
            Debug.WriteLine("Can't perform rebalance action today, will delay.");
          }
          else 
          {
            Rebalance();
          }

        }
        if (_account.InvestmentStage == InvestmentStage.Distributing)
        {
          if (InvestmentScheduleHelper.IsDistributionDay(_account.InvestmentSchedule, today))
          {
            Debug.WriteLine($"{today.ToString("MM-dd-yyyy")} is a distribution day");
            if (CanPerformTradeActionToday() == false)
            {
              _account.StackedActivities.Add(new ActivityHolder(Models.Action.Rebalance, 0, today));
              Debug.WriteLine("Can't perform rebalance action today, will delay.");
            }
            else 
            {
              Distribute(_account.InvestmentAmount);
              Debug.WriteLine("Need rules for distribution.");
            }
          }
        }
        foreach (var service in Assets) 
        {
          service.DividendReinvestment(today);
        }

    }

        private void HandleInvestmentStrategy()
        {
            _latestAction = InvestmentStrategyService.DetermineActionOnAccount();
            if (_latestAction == SuggestedAction.Buy)
            {
                if (_account.CashBalance > 0)
                {
                    _account.StackedActivities.Add(new ActivityHolder(Models.Action.Contribution, _account.CashBalance,  DateTimeService.GetInstance.GetCurrentDate()));
                    _account.CashBalance = 0;
                }
                /* release the hounds */
            }
            else if (_latestAction == SuggestedAction.Sell)
            { /* pull everything to a stacked attivity */
                decimal cumAmount = 0;
                foreach (var asset in Assets)
                {
                    if (asset.NumberOfShares > 0)
                    {
                        Debug.WriteLine($"Selling {asset.NumberOfShares} shares of {asset.TickerSymbol} at {asset.GetCurrentPrice()}");
                        _account.CashBalance += asset.NumberOfShares * asset.GetCurrentPrice();
                        asset.SellInDollars(asset.NumberOfShares * asset.GetCurrentPrice());
                    }
                }
            }
        }

        private void HandleCorporateActions(DateTime today)
        {
            /* splits, reverse splits, dividends, etc. */
            foreach (AssetService asset in Assets)
            {
                decimal dividend = asset.GetTotalDividendAmount(today);
                if (_account.DividentReinvestment)
                {
                    asset.BuyInDollars(dividend);
                }
                else
                {
                    _account.Dividends+=dividend;
                }
            }


            foreach (var asset in Assets)
            {
                asset.HandleSplit(today);
            }
            return;
        }
        private void HandleStackedActivities()
        {
            if ((_latestAction != SuggestedAction.Buy)||(_account.StackedActivities.Count == 0)) { return; }
            var  today = DateTimeService.GetInstance.GetCurrentDate();
      if (CanPerformTradeActionToday() == true)
      {
        foreach (var activity in _account.StackedActivities)
        {
          switch (activity.Action)
          {
            case Models.Action.Rebalance:
              Debug.WriteLine("Rebalancing as a result of missing it on: ", activity.DateOfRequest.Date);
              Rebalance();
              break;
            case Models.Action.Distribution:
              {
                Debug.WriteLine("Need rules for distribution.");
              }
              break;
            case Models.Action.Contribution:
              Debug.WriteLine($"Contributing on {today}as a result of missing it on:{activity.DateOfRequest.Date}, current balance: {GetBalance()}");
              Contribute(activity.Amount);
              Debug.WriteLine($"Balance after contribution of{activity.Amount} : {GetBalance()}");
              break;
            default:
              break;
          }
        }
        _account.StackedActivities.Clear();
      }
    }

        internal async Task<IEnumerable<object>> GetHoldingsByAccountIdAsync(int accountId)
        {
            throw new NotImplementedException();
        }
    }
}