using Microsoft.EntityFrameworkCore;
using NPlusOneDemo.Shop;

namespace NPlusOneDemo.Scenarios;

/// <summary>
/// No lazy loading at all: the loop lives in one layer and the query in another,
/// so nobody sees them together.
/// </summary>
public sealed class RepositoryInLoop : IScenario
{
    public string Title => "2. Repository call inside a loop";

    public async Task RunNaiveAsync(DemoShop shop)
    {
        await using var db = shop.CreateDb();
        var customers = new CustomerRepository(db);

        var orders = await db.Orders.AsNoTracking().ToListAsync();
        foreach (var order in orders)
        {
            var customer = await customers.GetByIdAsync(order.CustomerId); // one query per order
        }
    }

    public async Task RunFixedAsync(DemoShop shop)
    {
        await using var db = shop.CreateDb();
        var customers = new CustomerRepository(db);

        var orders = await db.Orders.AsNoTracking().ToListAsync();
        var byId = await customers.GetByIdsAsync(orders.Select(o => o.CustomerId).Distinct());
        foreach (var order in orders)
        {
            var customer = byId[order.CustomerId]; // in-memory lookup
        }
    }

    private sealed class CustomerRepository(ShopContext db)
    {
        public Task<Customer?> GetByIdAsync(int id) =>
            db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);

        // Contains becomes WHERE Id IN (...). For very large lists, split them into batches.
        public Task<Dictionary<int, Customer>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var list = ids.ToList();
            return db.Customers.AsNoTracking().Where(c => list.Contains(c.Id)).ToDictionaryAsync(c => c.Id);
        }
    }
}
