namespace RoamBudget.Models;

public class Member
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
}

public class Split
{
    public Guid MemberId { get; set; }
    public decimal Amount { get; set; }
}

public enum ExpenseType { Kitty, Custom }

public class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public Guid PaidById { get; set; }
    public ExpenseType Type { get; set; }
    public List<Split> Splits { get; set; } = new();
    public DateTime Date { get; set; } = DateTime.UtcNow;
}

public class Trip
{
    public string Name { get; set; } = "Our Trip";
    public List<Member> Members { get; set; } = new();
    public decimal KittyPerPerson { get; set; } = 200;
    public List<Expense> Expenses { get; set; } = new();
}

public record SetupRequest(string? TripName, decimal KittyPerPerson);
public record AddMemberRequest(string? Name);
public record AddExpenseRequest(
    string? Description,
    decimal Amount,
    Guid PaidById,
    ExpenseType Type,
    List<Guid>? IncludedMemberIds
);

public class TripState
{
    public string TripName { get; set; } = "";
    public decimal KittyPerPerson { get; set; }
    public decimal TotalKitty { get; set; }
    public decimal KittySpent { get; set; }
    public decimal KittyRemaining { get; set; }
    public List<Member> Members { get; set; } = new();
    public List<ExpenseDto> Expenses { get; set; } = new();
}

public class ExpenseDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public string PaidByName { get; set; } = "";
    public ExpenseType Type { get; set; }
    public string Date { get; set; } = "";
    public List<SplitDto> Splits { get; set; } = new();
}

public class SplitDto
{
    public string MemberName { get; set; } = "";
    public decimal Amount { get; set; }
}

public class SettlementResult
{
    public List<SettlementItem> Transactions { get; set; } = new();
    public List<BalanceSummary> Balances { get; set; } = new();
    public bool IsSettled { get; set; }
}

public class SettlementItem
{
    public string FromName { get; set; } = "";
    public string ToName { get; set; } = "";
    public decimal Amount { get; set; }
}

public class BalanceSummary
{
    public string MemberName { get; set; } = "";
    public decimal TotalPaid { get; set; }
    public decimal TotalOwed { get; set; }
    public decimal NetBalance { get; set; }
}
