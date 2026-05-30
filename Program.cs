using RoamBudget.Models;
using RoamBudget.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddSingleton<TripService>();

var app = builder.Build();
app.UseStaticFiles();
app.MapRazorPages();

// ── Trips ────────────────────────────────────────────────────
app.MapGet("/api/trips", (TripService svc) =>
    Results.Ok(svc.ListTrips()));

app.MapPost("/api/trips", (CreateTripRequest req, TripService svc) =>
{
    if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest("Name required");
    if (req.TotalBudget <= 0) return Results.BadRequest("Budget must be positive");
    return Results.Ok(svc.CreateTrip(req));
});

app.MapDelete("/api/trips/{id:guid}", (Guid id, TripService svc) =>
    svc.DeleteTrip(id) ? Results.NoContent() : Results.NotFound());

// ── Expenses ─────────────────────────────────────────────────
app.MapGet("/api/trips/{id:guid}/expenses", (Guid id, TripService svc) =>
{
    var detail = svc.GetTripDetail(id);
    return detail is null ? Results.NotFound() : Results.Ok(detail);
});

app.MapPost("/api/trips/{id:guid}/expenses", (Guid id, AddExpenseRequest req, TripService svc) =>
{
    if (string.IsNullOrWhiteSpace(req.Description) || req.Amount <= 0)
        return Results.BadRequest("Description and positive amount required");
    var detail = svc.AddExpense(id, req);
    return detail is null ? Results.NotFound() : Results.Ok(detail);
});

app.MapDelete("/api/trips/{id:guid}/expenses/{eid:guid}", (Guid id, Guid eid, TripService svc) =>
{
    var detail = svc.DeleteExpense(id, eid);
    return detail is null ? Results.NotFound() : Results.Ok(detail);
});

// ── Budget ───────────────────────────────────────────────────
app.MapGet("/api/trips/{id:guid}/budget", (Guid id, TripService svc) =>
{
    var budget = svc.GetBudget(id);
    return budget is null ? Results.NotFound() : Results.Ok(budget);
});

app.MapPut("/api/trips/{id:guid}/category-budgets", (Guid id, CategoryBudgetsRequest req, TripService svc) =>
    svc.UpdateCategoryBudgets(id, req) ? Results.Ok() : Results.NotFound());

// ── Profile ───────────────────────────────────────────────────
app.MapGet("/api/stats", (TripService svc) => Results.Ok(svc.GetStats()));

app.Run();

public partial class Program { }
