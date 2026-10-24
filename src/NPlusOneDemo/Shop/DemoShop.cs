using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NPlusOneDemo.Diagnostics;

namespace NPlusOneDemo.Shop;

public record RunResult(int DbQueries, long RowsRead, int HttpCalls, TimeSpan Elapsed)
{
    public int RoundTrips => DbQueries + HttpCalls;
}

/// <summary>
/// An in-memory SQLite shop plus a fake catalog API, both counting their round trips.
/// </summary>
public sealed class DemoShop : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private DemoShop(SqliteConnection connection, RoundTripCounter counter)
    {
        _connection = connection;
        Counter = counter;
    }

    public RoundTripCounter Counter { get; }
    public HttpClient Catalog { get; private set; } = null!;

    public static async Task<DemoShop> CreateAsync(int orders, TimeSpan dbLatency, TimeSpan httpLatency)
    {
        // The in-memory database lives as long as this connection stays open.
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var shop = new DemoShop(connection, new RoundTripCounter());

        await using (var db = shop.CreateDb())
        {
            await db.Database.EnsureCreatedAsync();
            await Seeder.SeedAsync(db, orders);
            var products = await db.Products.ToDictionaryAsync(p => p.Id, p => new CatalogProduct(p.Id, p.Name, p.Price));
            shop.Catalog = new HttpClient(new FakeCatalogApi(products, shop.Counter, httpLatency))
            {
                BaseAddress = new Uri("http://catalog/"),
            };
        }

        shop.Counter.DbLatency = dbLatency;
        return shop;
    }

    public ShopContext CreateDb(bool lazyLoading = false)
    {
        var options = new DbContextOptionsBuilder<ShopContext>()
            .UseSqlite(_connection)
            .AddInterceptors(Counter);
        if (lazyLoading)
            options.UseLazyLoadingProxies();
        return new ShopContext(options.Options);
    }

    public async Task<RunResult> MeasureAsync(Func<DemoShop, Task> run)
    {
        Counter.Reset();
        var clock = Stopwatch.StartNew();
        await run(this);
        clock.Stop();
        return new RunResult(Counter.DbQueries, Counter.RowsRead, Counter.HttpCalls, clock.Elapsed);
    }

    public async ValueTask DisposeAsync()
    {
        Catalog?.Dispose();
        await _connection.DisposeAsync();
    }
}
