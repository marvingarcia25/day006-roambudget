using RoamBudget.Models;

namespace RoamBudget.Services;

public class TripService
{
    private readonly List<Trip> _trips = new();
    private int _nextCoverIndex;
    private readonly object _lock = new();

    public TripService()
    {
        // Seed demo: Mangawhai Escape — client-side JS will fetch the Wikipedia photo
        _trips.Add(new Trip
        {
            Name = "Mangawhai Escape",
            DateRange = "14 – 16 Jun 2026",
            TotalBudget = 800,
            CoverIndex = 3,
            CategoryBudgets = new()
            {
                [Category.Accommodation] = 350,
                [Category.Food]          = 200,
                [Category.Activities]    = 150,
                [Category.Transport]     = 100
            },
            Expenses = new()
            {
                new() { Description = "Batch house",      Amount = 320m, Category = Category.Accommodation, Date = DateTime.UtcNow.AddDays(-2) },
                new() { Description = "Surf lesson",      Amount = 95m,  Category = Category.Activities,    Date = DateTime.UtcNow.AddDays(-2) },
                new() { Description = "The Smoke Shack",  Amount = 62m,  Category = Category.Food,          Date = DateTime.UtcNow.AddDays(-1) },
                new() { Description = "Petrol",           Amount = 48m,  Category = Category.Transport,     Date = DateTime.UtcNow.AddDays(-1) },
                new() { Description = "Fish & chips",     Amount = 28m,  Category = Category.Food,          Date = DateTime.UtcNow }
            }
        });
        _nextCoverIndex = 1;
    }

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
            Name       = req.Name.Trim(),
            DateRange  = req.DateRange?.Trim() ?? "",
            TotalBudget= Math.Max(0, req.TotalBudget),
            CoverIndex = req.CoverIndex ?? (_nextCoverIndex++ % 8),
            PhotoUrl   = req.PhotoUrl?.Trim(),
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
                Amount      = Math.Round(req.Amount, 2),
                Category    = req.Category,
                Date        = req.Date?.ToUniversalTime() ?? DateTime.UtcNow
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
            var remaining  = trip.TotalBudget - totalSpent;
            var spentPct   = trip.TotalBudget > 0
                ? (int)Math.Round(totalSpent / trip.TotalBudget * 100) : 0;

            var categories = Enum.GetValues<Category>().Select(cat =>
            {
                var spent = trip.Expenses.Where(e => e.Category == cat).Sum(e => e.Amount);
                trip.CategoryBudgets.TryGetValue(cat, out var budget);
                var catPct = budget > 0 ? (int)Math.Round(spent / budget * 100) : 0;
                return new CategoryBudgetDto
                {
                    Category     = cat,
                    CategoryName = CategoryMeta.Name(cat),
                    CategoryIcon = CategoryMeta.Icon(cat),
                    CategoryTint = CategoryMeta.Tint(cat),
                    Budget       = budget,
                    Spent        = spent,
                    Remaining    = budget > 0 ? budget - spent : 0,
                    SpentPct     = catPct
                };
            }).ToList();

            return new BudgetOverviewDto
            {
                TripId = trip.Id, TripName = trip.Name,
                TotalBudget = trip.TotalBudget, TotalSpent = totalSpent,
                Remaining = remaining, SpentPct = spentPct,
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
                .SelectMany(t => t.Expenses.Select(e => (Trip: t, Expense: e)))
                .OrderByDescending(x => x.Expense.Date)
                .Take(5)
                .Select(x => ToExpenseDto(x.Expense, x.Trip.Name))
                .ToList();

            return new AllTripsStats
            {
                TripCount        = _trips.Count,
                ExpenseCount     = _trips.Sum(t => t.Expenses.Count),
                TotalSpent       = _trips.Sum(t => t.Expenses.Sum(e => e.Amount)),
                RecentExpenses   = recent
            };
        }
    }

    // ── Helpers ──────────────────────────────────────────────
    private static TripSummaryDto Summary(Trip t)
    {
        var spent = t.Expenses.Sum(e => e.Amount);
        var pct   = t.TotalBudget > 0 ? (int)Math.Round(spent / t.TotalBudget * 100) : 0;
        return new TripSummaryDto
        {
            Id = t.Id, Name = t.Name, DateRange = t.DateRange,
            TotalBudget = t.TotalBudget, TotalSpent = spent,
            Remaining = t.TotalBudget - spent, SpentPct = pct,
            CoverIndex = t.CoverIndex, PhotoUrl = t.PhotoUrl,
            ExpenseCount = t.Expenses.Count
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
            CoverIndex = s.CoverIndex, PhotoUrl = s.PhotoUrl,
            ExpenseCount = s.ExpenseCount,
            Expenses = t.Expenses.OrderByDescending(e => e.Date)
                                 .Select(e => ToExpenseDto(e, null))
                                 .ToList()
        };
    }

    private static ExpenseDto ToExpenseDto(Expense e, string? tripName) => new()
    {
        Id = e.Id, Description = e.Description, Amount = e.Amount,
        Category = e.Category, CategoryName = CategoryMeta.Name(e.Category),
        CategoryIcon = CategoryMeta.Icon(e.Category), CategoryTint = CategoryMeta.Tint(e.Category),
        Date = e.Date.ToLocalTime().ToString("dd MMM yyyy"),
        TripName = tripName
    };
}
