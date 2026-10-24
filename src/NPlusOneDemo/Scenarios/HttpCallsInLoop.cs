using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using NPlusOneDemo.Shop;

namespace NPlusOneDemo.Scenarios;

/// <summary>The same pattern against another service: one HTTP call per item.</summary>
public sealed class HttpCallsInLoop : IScenario
{
    public const int OrderCount = 20;

    public string Title => "5. HTTP calls inside a loop";

    public async Task RunNaiveAsync(DemoShop shop)
    {
        var items = await LoadItemsAsync(shop);

        var total = 0m;
        foreach (var item in items)
        {
            var product = await shop.Catalog.GetFromJsonAsync<CatalogProduct>($"products/{item.ProductId}");
            total += item.Quantity * product!.Price;
        }
    }

    public async Task RunFixedAsync(DemoShop shop)
    {
        var items = await LoadItemsAsync(shop);

        var ids = string.Join(',', items.Select(i => i.ProductId).Distinct());
        var products = (await shop.Catalog.GetFromJsonAsync<List<CatalogProduct>>($"products?ids={ids}"))!
            .ToDictionary(p => p.Id);

        var total = 0m;
        foreach (var item in items)
            total += item.Quantity * products[item.ProductId].Price;
    }

    private static async Task<List<OrderItem>> LoadItemsAsync(DemoShop shop)
    {
        await using var db = shop.CreateDb();
        return await db.OrderItems.AsNoTracking().Where(i => i.OrderId <= OrderCount).ToListAsync();
    }
}
