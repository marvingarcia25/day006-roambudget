using RoamBudget.Models;
using RoamBudget.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddSingleton<TripService>();

var app = builder.Build();
app.UseStaticFiles();
app.MapRazorPages();

app.MapPost("/api/setup", (SetupRequest req, TripService svc) =>
{
    svc.UpdateSetup(req.TripName?.Trim() ?? "Our Trip", req.KittyPerPerson);
    return Results.Ok(svc.GetState());
});

app.MapPost("/api/members", (AddMemberRequest req, TripService svc) =>
{
    if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest("Name required");
    var member = svc.AddMember(req.Name.Trim());
    return Results.Ok(member);
});

app.MapDelete("/api/members/{id:guid}", (Guid id, TripService svc) =>
{
    svc.RemoveMember(id);
    return Results.Ok();
});

app.MapPost("/api/expenses", (AddExpenseRequest req, TripService svc) =>
{
    if (string.IsNullOrWhiteSpace(req.Description) || req.Amount <= 0)
        return Results.BadRequest("Description and positive amount required");
    svc.AddExpense(req);
    return Results.Ok(svc.GetState());
});

app.MapDelete("/api/expenses/{id:guid}", (Guid id, TripService svc) =>
{
    svc.RemoveExpense(id);
    return Results.Ok();
});

app.MapGet("/api/state", (TripService svc) => Results.Ok(svc.GetState()));
app.MapGet("/api/settle", (TripService svc) => Results.Ok(svc.GetSettlement()));

app.Run();

public partial class Program { }
