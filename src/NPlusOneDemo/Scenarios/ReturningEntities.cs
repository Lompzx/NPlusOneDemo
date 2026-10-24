using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NPlusOneDemo.Shop;

namespace NPlusOneDemo.Scenarios;

/// <summary>
/// A controller returns entities: the serializer walks every navigation and lazy loading
/// fetches each one on demand.
/// </summary>
public sealed class ReturningEntities : IScenario
{
    public const int PageSize = 100;

    public string Title => "3. Returning entities from the API";

    public async Task RunNaiveAsync(DemoShop shop)
    {
        await using var db = shop.CreateDb(lazyLoading: true);

        var page = await db.Orders.OrderBy(o => o.Id).Take(PageSize).ToListAsync();
        var json = JsonSerializer.Serialize(page); // what the controller does on return
    }

    public async Task RunFixedAsync(DemoShop shop)
    {
        await using var db = shop.CreateDb();

        // Project into response DTOs. AsSplitQuery loads the two collections in separate
        // queries instead of one query that multiplies items by payments (see scenario 6).
        var page = await db.Orders
            .OrderBy(o => o.Id)
            .Take(PageSize)
            .Select(o => new OrderResponse(
                o.Id,
                o.CreatedAt,
                o.Customer.Name,
                o.Items.Select(i => new OrderItemResponse(i.Product.Name, i.Quantity, i.UnitPrice)).ToList(),
                o.Payments.Select(p => new PaymentResponse(p.Method, p.Amount)).ToList()))
            .AsSplitQuery()
            .ToListAsync();
        var json = JsonSerializer.Serialize(page);
    }

    private record OrderResponse(
        int Id, DateTime CreatedAt, string Customer, List<OrderItemResponse> Items, List<PaymentResponse> Payments);

    private record OrderItemResponse(string Product, int Quantity, decimal UnitPrice);

    private record PaymentResponse(string Method, decimal Amount);
}
