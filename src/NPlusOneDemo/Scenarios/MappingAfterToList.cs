using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NPlusOneDemo.Shop;

namespace NPlusOneDemo.Scenarios;

/// <summary>
/// The same mapping, applied in memory after ToList or inside the query.
/// The second form is what AutoMapper's ProjectTo does.
/// </summary>
public sealed class MappingAfterToList : IScenario
{
    public const int PageSize = 100;

    private static readonly Expression<Func<Order, OrderSummary>> ToSummary = o =>
        new OrderSummary(o.Id, o.Customer.Name, o.Items.Count, o.Payments.Sum(p => p.Amount));

    private static readonly Func<Order, OrderSummary> ToSummaryInMemory = ToSummary.Compile();

    public string Title => "4. Mapping after ToList";

    public async Task RunNaiveAsync(DemoShop shop)
    {
        await using var db = shop.CreateDb(lazyLoading: true);

        var orders = await db.Orders.OrderBy(o => o.Id).Take(PageSize).ToListAsync();
        var summaries = orders.Select(ToSummaryInMemory).ToList(); // three lazy loads per order
    }

    public async Task RunFixedAsync(DemoShop shop)
    {
        await using var db = shop.CreateDb();

        var summaries = await db.Orders.OrderBy(o => o.Id).Take(PageSize).Select(ToSummary).ToListAsync();
    }

    private record OrderSummary(int Id, string Customer, int ItemCount, decimal Paid);
}
