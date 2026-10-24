using Microsoft.EntityFrameworkCore;
using NPlusOneDemo.Shop;

namespace NPlusOneDemo.Scenarios;

/// <summary>
/// Fixing the N+1 by including every collection in one query. The SQL joins items and
/// payments, so each order comes back items × payments times (cartesian explosion).
/// </summary>
public sealed class IncludeEverything : IScenario
{
    public const int PageSize = 200;

    public string Title => "6. Include everything (cartesian explosion)";

    public async Task RunNaiveAsync(DemoShop shop)
    {
        await using var db = shop.CreateDb();

        var orders = await Query(db).ToListAsync();
    }

    public async Task RunFixedAsync(DemoShop shop)
    {
        await using var db = shop.CreateDb();

        // One query per collection: a couple more round trips, far fewer rows.
        var orders = await Query(db).AsSplitQuery().ToListAsync();
    }

    private static IQueryable<Order> Query(ShopContext db) =>
        db.Orders
            .AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Payments)
            .OrderBy(o => o.Id)
            .Take(PageSize);
}
