using RoamBudget.Models;

namespace RoamBudget.Services;

public class TripService
{
    private readonly Trip _trip = new();
    private readonly object _lock = new();

    public void UpdateSetup(string name, decimal kittyPerPerson)
    {
        lock (_lock)
        {
            _trip.Name = name;
            _trip.KittyPerPerson = Math.Max(0, kittyPerPerson);
        }
    }

    public Member AddMember(string name)
    {
        var member = new Member { Name = name };
        lock (_lock) _trip.Members.Add(member);
        return member;
    }

    public void RemoveMember(Guid id)
    {
        lock (_lock) _trip.Members.RemoveAll(m => m.Id == id);
    }

    public void AddExpense(AddExpenseRequest req)
    {
        lock (_lock)
        {
            var included = req.IncludedMemberIds?.Count > 0
                ? _trip.Members.Where(m => req.IncludedMemberIds.Contains(m.Id)).ToList()
                : _trip.Members.ToList();

            if (included.Count == 0) included = _trip.Members.ToList();

            var shareAmount = included.Count > 0 ? Math.Round(req.Amount / included.Count, 2) : req.Amount;
            var splits = included.Select((m, i) => new Split
            {
                MemberId = m.Id,
                Amount = i == 0 ? req.Amount - shareAmount * (included.Count - 1) : shareAmount
            }).ToList();

            _trip.Expenses.Add(new Expense
            {
                Description = req.Description ?? "",
                Amount = req.Amount,
                PaidById = req.PaidById,
                Type = req.Type,
                Splits = splits
            });
        }
    }

    public void RemoveExpense(Guid id)
    {
        lock (_lock) _trip.Expenses.RemoveAll(e => e.Id == id);
    }

    public TripState GetState()
    {
        lock (_lock)
        {
            var kittySpent = _trip.Expenses
                .Where(e => e.Type == ExpenseType.Kitty)
                .Sum(e => e.Amount);

            return new TripState
            {
                TripName = _trip.Name,
                KittyPerPerson = _trip.KittyPerPerson,
                TotalKitty = _trip.KittyPerPerson * _trip.Members.Count,
                KittySpent = kittySpent,
                KittyRemaining = _trip.KittyPerPerson * _trip.Members.Count - kittySpent,
                Members = _trip.Members.ToList(),
                Expenses = _trip.Expenses
                    .OrderByDescending(e => e.Date)
                    .Select(e => new ExpenseDto
                    {
                        Id = e.Id,
                        Description = e.Description,
                        Amount = e.Amount,
                        PaidByName = _trip.Members.FirstOrDefault(m => m.Id == e.PaidById)?.Name ?? "?",
                        Type = e.Type,
                        Date = e.Date.ToString("dd MMM"),
                        Splits = e.Splits.Select(s => new SplitDto
                        {
                            MemberName = _trip.Members.FirstOrDefault(m => m.Id == s.MemberId)?.Name ?? "?",
                            Amount = s.Amount
                        }).ToList()
                    }).ToList()
            };
        }
    }

    public SettlementResult GetSettlement()
    {
        lock (_lock)
        {
            var balances = _trip.Members.ToDictionary(m => m.Id, _ => 0m);
            var totalPaid = _trip.Members.ToDictionary(m => m.Id, _ => 0m);
            var totalOwed = _trip.Members.ToDictionary(m => m.Id, _ => 0m);

            foreach (var expense in _trip.Expenses)
            {
                if (balances.ContainsKey(expense.PaidById))
                {
                    balances[expense.PaidById] += expense.Amount;
                    totalPaid[expense.PaidById] += expense.Amount;
                }
                foreach (var split in expense.Splits)
                {
                    if (balances.ContainsKey(split.MemberId))
                    {
                        balances[split.MemberId] -= split.Amount;
                        totalOwed[split.MemberId] += split.Amount;
                    }
                }
            }

            var summaries = _trip.Members.Select(m => new BalanceSummary
            {
                MemberName = m.Name,
                TotalPaid = totalPaid[m.Id],
                TotalOwed = totalOwed[m.Id],
                NetBalance = balances[m.Id]
            }).ToList();

            // Debt minimization (greedy creditor/debtor matching)
            var credits = balances.Where(kv => kv.Value > 0.005m)
                .Select(kv => (Id: kv.Key, Amt: kv.Value)).OrderByDescending(x => x.Amt).ToList();
            var debts = balances.Where(kv => kv.Value < -0.005m)
                .Select(kv => (Id: kv.Key, Amt: Math.Abs(kv.Value))).OrderByDescending(x => x.Amt).ToList();

            var creditAmts = credits.Select(c => c.Amt).ToList();
            var debtAmts = debts.Select(d => d.Amt).ToList();

            var transactions = new List<SettlementItem>();
            int ci = 0, di = 0;
            while (ci < credits.Count && di < debts.Count)
            {
                var transfer = Math.Round(Math.Min(creditAmts[ci], debtAmts[di]), 2);
                if (transfer > 0.005m)
                {
                    var from = _trip.Members.First(m => m.Id == debts[di].Id);
                    var to = _trip.Members.First(m => m.Id == credits[ci].Id);
                    transactions.Add(new SettlementItem { FromName = from.Name, ToName = to.Name, Amount = transfer });
                }
                creditAmts[ci] -= transfer;
                debtAmts[di] -= transfer;
                if (creditAmts[ci] < 0.01m) ci++;
                if (debtAmts[di] < 0.01m) di++;
            }

            return new SettlementResult
            {
                Transactions = transactions,
                Balances = summaries,
                IsSettled = !transactions.Any()
            };
        }
    }
}
