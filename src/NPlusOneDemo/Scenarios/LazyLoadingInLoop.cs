using Microsoft.EntityFrameworkCore;
using NPlusOneDemo.Shop;

namespace NPlusOneDemo.Scenarios;

/// <summary>The textbook case: a navigation read inside a loop, with lazy loading on.</summary>
public sealed class LazyLoadingInLoop : IScenario
{
    public string Title => "1. Lazy loading in a loop";

    public async Task RunNaiveAsync(DemoShop shop)
    {
        await using var db = shop.CreateDb(lazyLoading: true);

        var orders = await db.Orders.ToListAsync();
        var total = 0m;
        foreach (var order in orders)
            total += order.Items.Sum(i => i.Quantity * i.UnitPrice); // one query per order
    }

    public async Task RunFixedAsync(DemoShop shop)
    {
        await using var db = shop.CreateDb();

        // The sum runs in the database: one query for all orders.
        var totals = await db.Orders
            .Select(o => new { o.Id, Total = o.Items.Sum(i => i.Quantity * i.UnitPrice) })
            .ToListAsync();
    }
}
