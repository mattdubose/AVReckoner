using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Reckoner.ViewModels
{
    // One editable row in the account list — Balance is whatever's being entered for the
    // currently selected SnapshotDate, pre-filled with the most recent known value.
    public partial class NetWorthAccountRow : ObservableObject
    {
        public string AccountId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public NetWorthCategory Category { get; set; }
        [ObservableProperty] private decimal balance;
    }

    // One editable row in the physical-assets list. InterestRatePercent is 0-100 for display,
    // same convention as everywhere else — SaveSnapshot divides by 100 before persisting.
    public partial class PhysicalAssetRow : ObservableObject
    {
        public string AssetId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        [ObservableProperty] private decimal value;
        [ObservableProperty] private decimal debt;
        [ObservableProperty] private decimal interestRatePercent;
        public decimal Equity => Value - Debt;
    }

    // A display-only grouping of the same NetWorthAccountRow instances used for editing —
    // Cash/Non-Invested, Brokerage, and Retirement (Pre-Tax + Tax-Free combined, since both read
    // as "retirement accounts" to a client even though they're taxed differently), each with its
    // own subtotal. Rebuilt whenever Rows changes rather than kept in sync incrementally.
    public class AccountGroupSummary
    {
        public string GroupName { get; set; } = string.Empty;
        public List<NetWorthAccountRow> Rows { get; set; } = new();
        public decimal Subtotal => Rows.Sum(r => r.Balance);
    }

    public partial class NetWorthViewModel : BaseViewModel
    {
        private readonly AppStateService _appState;
        private readonly INetWorthRepository _repo;
        private int ClientId => _appState.CurrentClient?.ClientId ?? 0;

        public List<NetWorthCategory> Categories { get; } = Enum.GetValues<NetWorthCategory>().ToList();

        [ObservableProperty] private DateTimeOffset snapshotDate = new(DateTime.Today);
        [ObservableProperty] private ObservableCollection<NetWorthAccountRow> rows = new();
        [ObservableProperty] private decimal emergencyFundThreshold;

        [ObservableProperty] private string newAccountName = string.Empty;
        [ObservableProperty] private NetWorthCategory newAccountCategory;

        [ObservableProperty] private ObservableCollection<PhysicalAssetRow> physicalAssetRows = new();
        [ObservableProperty] private string newPhysicalAssetName = string.Empty;

        [ObservableProperty] private ObservableCollection<AccountGroupSummary> accountGroups = new();

        [ObservableProperty] private ISeries[] historySeries = Array.Empty<ISeries>();
        // A plain category axis, not a DateTimeAxis — snapshot dates are irregular (a few months
        // apart one time, ten months the next), so a true time-scaled axis stretches/squishes bars
        // unevenly. Evenly-spaced bars left-to-right, with the actual date visible on hover, reads
        // more honestly than pretending this is a continuous timeline.
        [ObservableProperty] private Axis[] historyXAxes = new Axis[]
        {
            new Axis { LabelsRotation = -45, TextSize = 11 }
        };
        [ObservableProperty] private Axis[] historyYAxes = new Axis[]
        {
            new Axis { Labeler = value => value.ToString("C0"), TextSize = 11 }
        };

        [ObservableProperty] private ISeries[] breakdownSeries = Array.Empty<ISeries>();

        // Bucket totals for the currently loaded SnapshotDate — same math as "Balancing your
        // Accounts": Cash minus the emergency-fund threshold is what's actually excess, and the
        // grand total only counts the four liquid buckets (Other/home-equity sits outside it).
        public decimal CashTotal => Rows.Where(r => r.Category == NetWorthCategory.Cash).Sum(r => r.Balance);
        public decimal ExcessCash => CashTotal - EmergencyFundThreshold;
        public decimal TaxableTotal => Rows.Where(r => r.Category == NetWorthCategory.TaxableInvested).Sum(r => r.Balance);
        public decimal PreTaxTotal => Rows.Where(r => r.Category == NetWorthCategory.PreTax).Sum(r => r.Balance);
        public decimal TaxFreeTotal => Rows.Where(r => r.Category == NetWorthCategory.TaxFree).Sum(r => r.Balance);
        public decimal OtherTotal => Rows.Where(r => r.Category == NetWorthCategory.Other).Sum(r => r.Balance);
        public decimal GrandTotalLiquid => CashTotal + TaxableTotal + PreTaxTotal + TaxFreeTotal;

        // The holistic picture — every physical asset's equity (not raw value) plus every liquid
        // bucket, so a house with a mortgage doesn't overstate what the household actually has.
        public decimal PhysicalAssetsTotal => PhysicalAssetRows.Sum(r => r.Value);
        public decimal DebtTotal => PhysicalAssetRows.Sum(r => r.Debt);
        public decimal PhysicalAssetsNetEquity => PhysicalAssetsTotal - DebtTotal;
        public decimal TotalNetWorth => GrandTotalLiquid + PhysicalAssetsNetEquity;

        public string NarrativeSummary
        {
            get
            {
                var dateText = SnapshotDate.DateTime.ToString("MMMM yyyy");
                var sb = new StringBuilder();
                sb.Append($"As of {dateText}, your household is worth {TotalNetWorth:C0}. ");

                if (GrandTotalLiquid > 0)
                    sb.Append($"{GrandTotalLiquid:C0} is invested — split across cash, taxable, pre-tax, and Roth accounts. ");

                if (PhysicalAssetsTotal > 0)
                {
                    var topAsset = PhysicalAssetRows.OrderByDescending(r => r.Value).FirstOrDefault();
                    var mostlyText = topAsset != null ? $", primarily {topAsset.Name}" : "";
                    sb.Append($"{PhysicalAssetsTotal:C0} is physical assets{mostlyText}. ");
                }

                sb.Append(DebtTotal > 0
                    ? $"You're carrying {DebtTotal:C0} in debt."
                    : "You're carrying no debt.");

                return sb.ToString();
            }
        }

        public NetWorthViewModel(AppShellService appShell, AppStateService appState, INetWorthRepository repo) : base(appShell)
        {
            _appState = appState;
            _repo = repo;
            LoadForSnapshotDate();
            RebuildHistoryChart();
        }

        partial void OnSnapshotDateChanged(DateTimeOffset value) => LoadForSnapshotDate();

        private void LoadForSnapshotDate()
        {
            var accounts = _repo.GetAccounts(ClientId);
            var balances = _repo.GetBalanceEntries(ClientId);
            var thresholds = _repo.GetEmergencyFundThresholds(ClientId);
            var physicalAssets = _repo.GetPhysicalAssets(ClientId);
            var physicalValues = _repo.GetPhysicalAssetValueEntries(ClientId);
            var asOf = SnapshotDate.DateTime.Date;

            foreach (var row in Rows)
                row.PropertyChanged -= Row_PropertyChanged;

            Rows = new ObservableCollection<NetWorthAccountRow>(accounts.Select(a => new NetWorthAccountRow
            {
                AccountId = a.Id,
                Name = a.Name,
                Category = a.Category,
                Balance = LatestValueAsOf(balances.Where(b => b.AccountId == a.Id), b => b.Date, b => b.Balance, asOf),
            }));
            foreach (var row in Rows)
                row.PropertyChanged += Row_PropertyChanged;

            foreach (var row in PhysicalAssetRows)
                row.PropertyChanged -= PhysicalAssetRow_PropertyChanged;

            PhysicalAssetRows = new ObservableCollection<PhysicalAssetRow>(physicalAssets.Select(a =>
            {
                var entries = physicalValues.Where(v => v.AssetId == a.Id).ToList();
                return new PhysicalAssetRow
                {
                    AssetId = a.Id,
                    Name = a.Name,
                    Value = LatestValueAsOf(entries, v => v.Date, v => v.Value, asOf),
                    Debt = LatestValueAsOf(entries, v => v.Date, v => v.Debt, asOf),
                    InterestRatePercent = LatestValueAsOf(entries, v => v.Date, v => v.InterestRate, asOf) * 100m,
                };
            }));
            foreach (var row in PhysicalAssetRows)
                row.PropertyChanged += PhysicalAssetRow_PropertyChanged;

            EmergencyFundThreshold = LatestValueAsOf(thresholds, t => t.Date, t => t.Amount, asOf);
            RecomputeTotals();
        }

        private void Row_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => RecomputeTotals();
        private void PhysicalAssetRow_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => RecomputeTotals();

        partial void OnEmergencyFundThresholdChanged(decimal value) => RecomputeTotals();

        private void RecomputeTotals()
        {
            OnPropertyChanged(nameof(CashTotal));
            OnPropertyChanged(nameof(ExcessCash));
            OnPropertyChanged(nameof(TaxableTotal));
            OnPropertyChanged(nameof(PreTaxTotal));
            OnPropertyChanged(nameof(TaxFreeTotal));
            OnPropertyChanged(nameof(OtherTotal));
            OnPropertyChanged(nameof(GrandTotalLiquid));
            OnPropertyChanged(nameof(PhysicalAssetsTotal));
            OnPropertyChanged(nameof(DebtTotal));
            OnPropertyChanged(nameof(PhysicalAssetsNetEquity));
            OnPropertyChanged(nameof(TotalNetWorth));
            OnPropertyChanged(nameof(NarrativeSummary));
            RebuildAccountGroups();
            RebuildBreakdownChart();
        }

        // Cash/Non-Invested, Brokerage, Retirement (Pre-Tax + Tax-Free together) — Other only
        // shows up as its own group when something's actually in it, so an empty bucket doesn't
        // clutter the page.
        private void RebuildAccountGroups()
        {
            (string Name, NetWorthCategory[] Categories)[] groupDefs =
            {
                ("Cash / Non-Invested", new[] { NetWorthCategory.Cash }),
                ("Brokerage Account", new[] { NetWorthCategory.TaxableInvested }),
                ("Retirement Accounts", new[] { NetWorthCategory.PreTax, NetWorthCategory.TaxFree }),
                ("Other", new[] { NetWorthCategory.Other }),
            };

            var groups = groupDefs
                .Select(g => new AccountGroupSummary
                {
                    GroupName = g.Name,
                    Rows = Rows.Where(r => g.Categories.Contains(r.Category)).ToList(),
                })
                .Where(g => g.Rows.Count > 0)
                .ToList();

            AccountGroups = new ObservableCollection<AccountGroupSummary>(groups);
        }

        // The most recent entry at or before `asOf` — lets a snapshot date use whatever an
        // account's last known balance was, even if that particular account wasn't touched on
        // this exact date.
        private static decimal LatestValueAsOf<T>(IEnumerable<T> entries, Func<T, DateTime> dateOf, Func<T, decimal> valueOf, DateTime asOf) =>
            entries.Where(e => dateOf(e) <= asOf)
                   .OrderByDescending(dateOf)
                   .Select(valueOf)
                   .FirstOrDefault();

        [RelayCommand]
        private void AddAccount()
        {
            if (string.IsNullOrWhiteSpace(NewAccountName)) return;
            var account = _repo.AddAccount(ClientId, NewAccountName.Trim(), NewAccountCategory);
            var row = new NetWorthAccountRow { AccountId = account.Id, Name = account.Name, Category = account.Category };
            row.PropertyChanged += Row_PropertyChanged;
            Rows.Add(row);
            NewAccountName = string.Empty;
            RecomputeTotals();
        }

        [RelayCommand]
        private void RemoveAccount(NetWorthAccountRow row)
        {
            if (row == null) return;
            _repo.RemoveAccount(row.AccountId);
            row.PropertyChanged -= Row_PropertyChanged;
            Rows.Remove(row);
            RecomputeTotals();
            RebuildHistoryChart();
        }

        [RelayCommand]
        private void AddPhysicalAsset()
        {
            if (string.IsNullOrWhiteSpace(NewPhysicalAssetName)) return;
            var asset = _repo.AddPhysicalAsset(ClientId, NewPhysicalAssetName.Trim());
            var row = new PhysicalAssetRow { AssetId = asset.Id, Name = asset.Name };
            row.PropertyChanged += PhysicalAssetRow_PropertyChanged;
            PhysicalAssetRows.Add(row);
            NewPhysicalAssetName = string.Empty;
            RecomputeTotals();
        }

        [RelayCommand]
        private void RemovePhysicalAsset(PhysicalAssetRow row)
        {
            if (row == null) return;
            _repo.RemovePhysicalAsset(row.AssetId);
            row.PropertyChanged -= PhysicalAssetRow_PropertyChanged;
            PhysicalAssetRows.Remove(row);
            RecomputeTotals();
            RebuildHistoryChart();
        }

        [RelayCommand]
        private void SaveSnapshot()
        {
            var date = SnapshotDate.DateTime.Date;
            foreach (var row in Rows)
                _repo.SetBalance(row.AccountId, date, row.Balance);
            _repo.SetEmergencyFundThreshold(ClientId, date, EmergencyFundThreshold);
            foreach (var row in PhysicalAssetRows)
                _repo.SetPhysicalAssetValue(row.AssetId, date, row.Value, row.Debt, row.InterestRatePercent / 100m);
            RebuildHistoryChart();
        }

        // Current snapshot's buckets as a donut — the "whole picture at a glance" piece. Debt
        // isn't a slice (it's a subtraction, not a part of the whole), so it only shows up in the
        // headline Total Net Worth number and the narrative, not here.
        private void RebuildBreakdownChart()
        {
            (string Name, decimal Value, SKColor Color)[] slices =
            {
                ("Cash", CashTotal, SKColors.SteelBlue),
                ("Taxable Invested", TaxableTotal, SKColors.MediumSeaGreen),
                ("Pre-Tax", PreTaxTotal, SKColors.Goldenrod),
                ("Tax-Free (Roth)", TaxFreeTotal, SKColors.MediumPurple),
                ("Other", OtherTotal, SKColors.Gray),
                ("Physical Assets", PhysicalAssetsTotal, SKColors.SaddleBrown),
            };

            BreakdownSeries = slices
                .Where(s => s.Value > 0)
                .Select(s => (ISeries)new PieSeries<double>
                {
                    Name = s.Name,
                    Values = new[] { (double)s.Value },
                    Fill = new SolidColorPaint(s.Color),
                    ToolTipLabelFormatter = point => point.Coordinate.PrimaryValue.ToString("C2"),
                })
                .ToArray();
        }

        // One stacked column per saved date, one series per bucket — so growth (or which bucket
        // is actually driving it) is visible across every check-in, not just two snapshots
        // sitting side by side in separate spreadsheet tabs. Physical Assets stacks as a positive
        // band, Debt as a negative one, so the bars visibly extend below zero for however much is
        // owed — a household's real net worth trend, not just its liquid one.
        private void RebuildHistoryChart()
        {
            var accounts = _repo.GetAccounts(ClientId);
            var balances = _repo.GetBalanceEntries(ClientId);
            var physicalAssets = _repo.GetPhysicalAssets(ClientId);
            var physicalValues = _repo.GetPhysicalAssetValueEntries(ClientId);

            var dates = balances.Select(b => b.Date.Date)
                .Concat(physicalValues.Select(v => v.Date.Date))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            if (dates.Count == 0 || (accounts.Count == 0 && physicalAssets.Count == 0))
            {
                HistorySeries = Array.Empty<ISeries>();
                return;
            }

            decimal CategoryTotalAt(NetWorthCategory category, DateTime asOf) =>
                accounts.Where(a => a.Category == category)
                    .Sum(a => LatestValueAsOf(balances.Where(b => b.AccountId == a.Id), b => b.Date, b => b.Balance, asOf));

            decimal PhysicalAssetsTotalAt(DateTime asOf) =>
                physicalAssets.Sum(a => LatestValueAsOf(physicalValues.Where(v => v.AssetId == a.Id), v => v.Date, v => v.Value, asOf));

            decimal DebtTotalAt(DateTime asOf) =>
                physicalAssets.Sum(a => LatestValueAsOf(physicalValues.Where(v => v.AssetId == a.Id), v => v.Date, v => v.Debt, asOf));

            ISeries BuildSeries(string name, SKColor color, Func<DateTime, decimal> valueAt) => new StackedColumnSeries<double>
            {
                Name = name,
                Values = dates.Select(d => (double)valueAt(d)).ToArray(),
                Fill = new SolidColorPaint(color),
                Stroke = null,
            };

            HistorySeries = new[]
            {
                BuildSeries("Cash", SKColors.SteelBlue, d => CategoryTotalAt(NetWorthCategory.Cash, d)),
                BuildSeries("Taxable Invested", SKColors.MediumSeaGreen, d => CategoryTotalAt(NetWorthCategory.TaxableInvested, d)),
                BuildSeries("Pre-Tax", SKColors.Goldenrod, d => CategoryTotalAt(NetWorthCategory.PreTax, d)),
                BuildSeries("Tax-Free (Roth)", SKColors.MediumPurple, d => CategoryTotalAt(NetWorthCategory.TaxFree, d)),
                BuildSeries("Physical Assets", SKColors.SaddleBrown, PhysicalAssetsTotalAt),
                BuildSeries("Debt", SKColors.IndianRed, d => -DebtTotalAt(d)),
            };

            // Category axis — one evenly-spaced label per snapshot date, in order left-to-right.
            HistoryXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = dates.Select(d => d.ToString("MMM yyyy")).ToArray(),
                    LabelsRotation = -45,
                    TextSize = 11,
                }
            };
        }
    }
}
