using RoamBudget.Models;

namespace RoamBudget.Services;

public class TripService
{
    private readonly List<Trip> _trips = new();
    private int _nextCoverIndex;
    private readonly object _lock = new();

    // ── Trips ────────────────────────────────────────────────
    public List<TripSummaryDto> ListTrips()
    {
        lock (_lock)
            return _trips.OrderByDescending(t => t.CreatedAt).Select(Summary).ToList();
    }

    public TripSummaryDto CreateTrip(CreateTripRequest req)
    {
        var trip = new Trip
        {
            Name = req.Name.Trim(),
            DateRange = req.DateRange?.Trim() ?? "",
            TotalBudget = Math.Max(0, req.TotalBudget),
            CoverIndex = req.CoverIndex ?? (_nextCoverIndex++ % 8),
            CategoryBudgets = req.CategoryBudgets ?? new()
        };
        lock (_lock) _trips.Add(trip);
        return Summary(trip);
    }

    public bool DeleteTrip(Guid id)
    {
        lock (_lock) return _trips.RemoveAll(t => t.Id == id) > 0;
    }

    // ── Expenses ─────────────────────────────────────────────
    public TripDetailDto? GetTripDetail(Guid tripId)
    {
        lock (_lock)
        {
            var trip = _trips.FirstOrDefault(t => t.Id == tripId);
            return trip is null ? null : Detail(trip);
        }
    }

    public TripDetailDto? AddExpense(Guid tripId, AddExpenseRequest req)
    {
        lock (_lock)
        {
            var trip = _trips.FirstOrDefault(t => t.Id == tripId);
            if (trip is null) return null;
            trip.Expenses.Add(new Expense
            {
                Description = req.Description.Trim(),
                Amount = Math.Round(req.Amount, 2),
                Category = req.Category,
                Date = req.Date?.ToUniversalTime() ?? DateTime.UtcNow
            });
            return Detail(trip);
        }
    }

    public TripDetailDto? DeleteExpense(Guid tripId, Guid expenseId)
    {
        lock (_lock)
        {
            var trip = _trips.FirstOrDefault(t => t.Id == tripId);
            if (trip is null) return null;
            trip.Expenses.RemoveAll(e => e.Id == expenseId);
            return Detail(trip);
        }
    }

    // ── Budget ───────────────────────────────────────────────
    public BudgetOverviewDto? GetBudget(Guid tripId)
    {
        lock (_lock)
        {
            var trip = _trips.FirstOrDefault(t => t.Id == tripId);
            if (trip is null) return null;

            var totalSpent = trip.Expenses.Sum(e => e.Amount);
            var remaining = trip.TotalBudget - totalSpent;
            var spentPct = trip.TotalBudget > 0
                ? (int)Math.Round(totalSpent / trip.TotalBudget * 100)
                : 0;

            var categories = Enum.GetValues<Category>().Select(cat =>
            {
                var spent = trip.Expenses.Where(e => e.Category == cat).Sum(e => e.Amount);
                trip.CategoryBudgets.TryGetValue(cat, out var budget);
                var catRemaining = budget > 0 ? budget - spent : 0;
                var catPct = budget > 0 ? (int)Math.Round(spent / budget * 100) : 0;
                return new CategoryBudgetDto
                {
                    Category = cat,
                    CategoryName = CategoryMeta.Name(cat),
                    CategoryIcon = CategoryMeta.Icon(cat),
                    CategoryTint = CategoryMeta.Tint(cat),
                    Budget = budget,
                    Spent = spent,
                    Remaining = catRemaining,
                    SpentPct = catPct
                };
            }).ToList();

            return new BudgetOverviewDto
            {
                TripId = trip.Id,
                TripName = trip.Name,
                TotalBudget = trip.TotalBudget,
                TotalSpent = totalSpent,
                Remaining = remaining,
                SpentPct = spentPct,
                Categories = categories
            };
        }
    }

    public bool UpdateCategoryBudgets(Guid tripId, CategoryBudgetsRequest req)
    {
        lock (_lock)
        {
            var trip = _trips.FirstOrDefault(t => t.Id == tripId);
            if (trip is null) return false;
            trip.CategoryBudgets = req.Budgets ?? new();
            return true;
        }
    }

    // ── Profile stats ────────────────────────────────────────
    public AllTripsStats GetStats()
    {
        lock (_lock)
        {
            var recent = _trips
                .SelectMany(t => t.Expenses)
                .OrderByDescending(e => e.Date)
                .Take(5)
                .Select(ToExpenseDto)
                .ToList();

            return new AllTripsStats
            {
                TripCount = _trips.Count,
                ExpenseCount = _trips.Sum(t => t.Expenses.Count),
                TotalSpent = _trips.Sum(t => t.Expenses.Sum(e => e.Amount)),
                RecentExpenses = recent
            };
        }
    }

    // ── Helpers ──────────────────────────────────────────────
    private static TripSummaryDto Summary(Trip t)
    {
        var spent = t.Expenses.Sum(e => e.Amount);
        var remaining = t.TotalBudget - spent;
        var pct = t.TotalBudget > 0 ? (int)Math.Round(spent / t.TotalBudget * 100) : 0;
        return new TripSummaryDto
        {
            Id = t.Id, Name = t.Name, DateRange = t.DateRange,
            TotalBudget = t.TotalBudget, TotalSpent = spent,
            Remaining = remaining, SpentPct = pct,
            CoverIndex = t.CoverIndex, ExpenseCount = t.Expenses.Count
        };
    }

    private static TripDetailDto Detail(Trip t)
    {
        var s = Summary(t);
        return new TripDetailDto
        {
            Id = s.Id, Name = s.Name, DateRange = s.DateRange,
            TotalBudget = s.TotalBudget, TotalSpent = s.TotalSpent,
            Remaining = s.Remaining, SpentPct = s.SpentPct,
            CoverIndex = s.CoverIndex, ExpenseCount = s.ExpenseCount,
            Expenses = t.Expenses.OrderByDescending(e => e.Date).Select(ToExpenseDto).ToList()
        };
    }

    private static ExpenseDto ToExpenseDto(Expense e) => new()
    {
        Id = e.Id, Description = e.Description, Amount = e.Amount,
        Category = e.Category, CategoryName = CategoryMeta.Name(e.Category),
        CategoryIcon = CategoryMeta.Icon(e.Category), CategoryTint = CategoryMeta.Tint(e.Category),
        Date = e.Date.ToLocalTime().ToString("dd MMM yyyy")
    };
}
