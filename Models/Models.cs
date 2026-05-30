namespace RoamBudget.Models;

public enum Category
{
    Food = 0,
    Accommodation = 1,
    Transport = 2,
    Activities = 3,
    Shopping = 4,
    Other = 5
}

public static class CategoryMeta
{
    public static string Icon(Category c) => c switch
    {
        Category.Food          => "🍽️",
        Category.Accommodation => "🏨",
        Category.Transport     => "🚗",
        Category.Activities    => "🎭",
        Category.Shopping      => "🛍️",
        _                      => "💼"
    };

    public static string Name(Category c) => c.ToString();

    public static string Tint(Category c) => c switch
    {
        Category.Food          => "#FFF4E6",
        Category.Accommodation => "#E6F4FF",
        Category.Transport     => "#EFF6FF",
        Category.Activities    => "#F4F0FF",
        Category.Shopping      => "#FFF0F9",
        _                      => "#F0F4F8"
    };
}

public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "My Trip";
    public string DateRange { get; set; } = "";
    public decimal TotalBudget { get; set; }
    public Dictionary<Category, decimal> CategoryBudgets { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
    public int CoverIndex { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public Category Category { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
}

// ── Request DTOs ─────────────────────────────────────────────
public record CreateTripRequest(
    string Name,
    string? DateRange,
    decimal TotalBudget,
    int? CoverIndex,
    string? PhotoUrl,
    Dictionary<Category, decimal>? CategoryBudgets);

public record AddExpenseRequest(
    string Description,
    decimal Amount,
    Category Category,
    DateTime? Date);

public record CategoryBudgetsRequest(Dictionary<Category, decimal> Budgets);

// ── Response DTOs ────────────────────────────────────────────
public class TripSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string DateRange { get; set; } = "";
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal Remaining { get; set; }
    public int SpentPct { get; set; }
    public int CoverIndex { get; set; }
    public string? PhotoUrl { get; set; }
    public int ExpenseCount { get; set; }
}

public class ExpenseDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public Category Category { get; set; }
    public string CategoryName { get; set; } = "";
    public string CategoryIcon { get; set; } = "";
    public string CategoryTint { get; set; } = "";
    public string Date { get; set; } = "";
    public string? TripName { get; set; }
}

public class TripDetailDto : TripSummaryDto
{
    public List<ExpenseDto> Expenses { get; set; } = new();
}

public class CategoryBudgetDto
{
    public Category Category { get; set; }
    public string CategoryName { get; set; } = "";
    public string CategoryIcon { get; set; } = "";
    public string CategoryTint { get; set; } = "";
    public decimal Budget { get; set; }
    public decimal Spent { get; set; }
    public decimal Remaining { get; set; }
    public int SpentPct { get; set; }
}

public class BudgetOverviewDto
{
    public Guid TripId { get; set; }
    public string TripName { get; set; } = "";
    public decimal TotalBudget { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal Remaining { get; set; }
    public int SpentPct { get; set; }
    public List<CategoryBudgetDto> Categories { get; set; } = new();
}

public class AllTripsStats
{
    public int TripCount { get; set; }
    public int ExpenseCount { get; set; }
    public decimal TotalSpent { get; set; }
    public List<ExpenseDto> RecentExpenses { get; set; } = new();
}
