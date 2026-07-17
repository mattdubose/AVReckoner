# FWStrategy Investigation Notes

Working notes from a Mac-compatibility pass on this repo, which turned into a
real bug hunt for why the FWStrategy backtest ("buyback") produces different,
sometimes wildly wrong, results. Captured here so this can be resynced and
continued on the primary (Windows) machine.

## Branch map

- `master` / `origin/master` — unchanged, at `f023618`. No local work here.
- `lastWorking_38b2048` — checkout of commit `38b2048` ("cleanup", 2026-04-12),
  the last commit believed to produce trustworthy FWStrategy results, plus one
  commit that fixes a chart-crashing race condition (see Bug 1) so it can
  actually be run for comparison. Treat this as the baseline.
- `first-broken-4f0770d` — checkout of commit `4f0770d` ("REVIEW THESE
  CHANGES!!! I don't understand all of these."), the very next commit after
  `38b2048` and the first one suspected of introducing the regression. Also
  has the Bug 1 chart-crash fix committed, plus (uncommitted, stashed) a
  proof-of-concept for the Bug 4 fix below.
- `macWork` — branched from current `master` (`f023618`). This is the "real"
  working branch, intended to eventually merge back to `master`. Has commit
  `bf89876` with fixes for Bugs 1-3 below, plus an uncommitted fix for Bug 4.
  **Bug 5 (below) is found but not yet fixed anywhere.**

## Bugs found so far

### Bug 1 — Chart-crashing race condition (fixed, all branches)
`ViewModels/InvestmentPerformanceViewModel.cs`, inside `RunInvestmentSimulation`.
The background sim loop handed the *same* mutable `List<DateTimePoint>`
reference to the chart (`series.Values = allPoints`) while continuing to
`Add()` to it. LiveCharts renders asynchronously even after the dispatcher
call returns, so its render pass could still be enumerating the list while
the sim thread mutated it — `InvalidOperationException: Collection was
modified`. Fix: snapshot a copy (`new List<DateTimePoint>(allPoints)`) before
handing it to the chart each render. Two variants of this bug existed (one
per branch's version of the render loop — list-swap style on `4f0770d`+,
`ObservableCollection.Add` style on `38b2048`); both are fixed on their
respective branches.

### Bug 2 — Cross-contaminated per-day price cache (fixed on `macWork`)
`Services/AssetService.cs`. A per-day price cache was added between
`38b2048` and `4f0770d`, but `GetCurrentPrice()` and `GetLatestPrice()`
shared a single `_priceCacheDate` gate field instead of having independent
ones. Whichever method ran second on a given day incorrectly treated the
date match as proof its *own* cache was fresh, when only the *other*
method's cache had actually been refreshed that day — so `GetCurrentPrice()`
effectively froze at whatever value it first fetched, for the rest of the
sim. That stale price fed into `AccountService.Rebalance()`, which mixes a
fresh `totalBalance` with a stale per-asset price to compute share
adjustments — the mismatch compounds across rebalances, which is the "looks
normal for a while, then balance goes crazy high" symptom. Fixed by giving
each method its own cache-date field.

### Bug 3 — Error code used as a price (fixed on `macWork`)
`Services/HistoricalBasedMarketInterface.cs`. When no price data exists for
a date (e.g. simulation runs past the end of loaded history),
`GetCurrentPrice`/`GetLatestPrice` returned
`(decimal)_historicalDataIf.GetLastError()` — i.e. a negative enum value
like `-3` (`DateNotPresent`) used directly as a dollar price. This is what
caused the balance to crash toward/through zero instead of holding flat when
a sim's end date ran past the available data. Fixed to return `0` (the
codebase's existing "no valid price" sentinel) instead. Paired with a change
in `AssetService` to hold at the last known good price instead of
propagating `0`/negative values into `GetBalance()`.

### Bug 4 — Cache fast-path bounds check (found & fixed on `macWork`,
proof-of-concept stashed on `first-broken-4f0770d`)
`Repositories/CachingHistoricalStockData.cs`, `GetLatestDaysInfo`. New in
`4f0770d`. The fast path only checked that `startDate` (day 0 of the
lookback) was within the cached window, then walked backward up to
`maxLookback` (300) days assuming the whole range was cached — it wasn't
guaranteed to be. Once the cache window narrowed (which happens routinely —
see the note on `GetInfo`'s per-weekend reload behavior below), the loop
could walk past `_cachedStart` and silently return `null` instead of falling
back to the DB, even when real data existed further back. This feeds
`GetLatestPrice`, which is what the FWStrategy's selloff/buyback triggers
and the charted balance are computed from. Fixed by checking the *earliest*
date the loop can reach against `_cachedStart`, matching the (correct)
pattern already used in the sibling method `GetLastXDays`.

Known but *not* fixed: `CachingHistoricalStockData.GetInfo()` reloads its
entire cache window (±182 days, centered on whatever date was requested)
every time it's asked about a date not currently in the cache — which
includes every weekend/holiday during a sim, since those dates have no DB
row. This thrashes the cache set up by `PreloadForSimulation` and is a real
performance problem (defeats the purpose of preloading), though it hasn't
been proven to cause incorrect *values*, just repeated DB round-trips.
Worth revisiting.

### Bug 5 — Cross-run state leakage (found, NOT YET fixed)
This is the live issue. Reproduction: run "Buy & Hold" first, then run
"FWStrategy" in the same session — FWStrategy's balance starts around
$1300 instead of the expected ~$0/$100. Reverse the order and FWStrategy
looks right but Buy & Hold now inherits the inflated start instead.

Root cause: `InvestmentPerformanceViewModel` constructs a single
`AccountService` (wrapping a single `Account` and its `AssetService` list)
**once**, in the constructor, and reuses it for every scenario run across
the session. `SimulationSettingsViewModel.SetSelections()` (called at the
top of every run) does correctly reset share counts for the *active*
scenario's holdings and rebuild `AccountService.Assets` fresh from them —
but it does not reset:
- `Account.CashBalance` — never zeroed between runs.
- `Account.StackedActivities` — only ever appended to, never cleared, so an
  activity a prior run couldn't execute (deferred by
  `CanPerformTradeActionToday()`) rides into the next scenario's run and
  fires on day 1.
- `Account.Dividends` — never zeroed.
- `AccountService.AllTimeHigh` and the private `_latestAction` field — no
  reset path exists at all, since `AccountService` itself is never
  recreated.
- FWInvestmentStrategy's internal trigger state (`highsForEvaluation`,
  `_curState`, `TriggeredPrice`, etc.) — this one *is* handled correctly
  today, since `SetSelections()` constructs a fresh `FWInvestmentStrategy`
  per run when the active scenario uses it.

## Direction agreed for Bug 5

Separate concerns instead of patching individual reset points:

- **Market/historical-data layer** (`SqliteHistoricalStockData`,
  `CachingHistoricalStockData`, `HistoricalBasedMarketInterface`) is
  read-only with respect to simulation results and safe — good, even — to
  share/reuse across runs. Note it currently *isn't* reused: every call to
  `SLMarketSecurityHelper.BuildAssetServices(account)` constructs brand new
  instances of all of these from scratch, which wastefully rebuilds the DB
  cache on every scenario run instead of sharing it. Worth fixing for
  performance once the correctness issue is resolved.
- **Simulation result state** — `Account`'s mutable fields, `AccountService`'s
  tracking fields, and the `FWInvestmentStrategy` instance — must be a fresh
  instance constructed at the start of every `RunInvestmentSimulation()`
  call, not fields reset in place on a long-lived shared object. Reset-in-place
  is fragile (the next field added to `Account`/`AccountService` has to be
  remembered and reset too, or this exact bug reappears); fresh-instance-per-run
  can't leak by construction.

Plan: refactor `RunInvestmentSimulation()` (and/or `SetSelections()`) to build
a new `Account` + `AccountService` per run from an immutable "template"
(the scenario's holdings/settings config), while keeping the historical-data
provider construction reusable/shared across runs rather than rebuilt each
time.

## Status as of this note

- `macWork` has Bugs 1-3 committed (`bf89876`) and Bug 4 fixed but
  **uncommitted**.
- Bug 5 fix not yet implemented anywhere.
- Mac-compatibility goal (the original reason for this branch) is met — the
  app builds and runs on macOS.
