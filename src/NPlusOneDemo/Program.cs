using System.Globalization;
using NPlusOneDemo.Scenarios;
using NPlusOneDemo.Shop;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var orders = ReadOption("--orders", 2000);
var dbLatency = TimeSpan.FromMilliseconds(ReadOption("--db-latency-ms", 1));
var httpLatency = TimeSpan.FromMilliseconds(ReadOption("--http-latency-ms", 25));

Console.WriteLine($"Seeding {orders:N0} orders, {Seeder.CustomerCount} customers and {Seeder.ProductCount} products (SQLite in memory)...");
await using var shop = await DemoShop.CreateAsync(orders, dbLatency, httpLatency);
Console.WriteLine($"Simulated round trip: database {dbLatency.TotalMilliseconds} ms, HTTP {httpLatency.TotalMilliseconds} ms");
Console.WriteLine();

const string row = "{0,-44} {1,-6} {2,12} {3,12} {4,10}";
Console.WriteLine(row, "Scenario", "", "Round trips", "Rows read", "Time");
Console.WriteLine(new string('-', 88));

foreach (var scenario in IScenario.All)
{
    var naive = await shop.MeasureAsync(scenario.RunNaiveAsync);
    var fixedRun = await shop.MeasureAsync(scenario.RunFixedAsync);
    Console.WriteLine(row, scenario.Title, "naive", naive.RoundTrips.ToString("N0"), naive.RowsRead.ToString("N0"), Format(naive.Elapsed));
    Console.WriteLine(row, "", "fixed", fixedRun.RoundTrips.ToString("N0"), fixedRun.RowsRead.ToString("N0"), Format(fixedRun.Elapsed));
    Console.WriteLine();
}

static string Format(TimeSpan elapsed) =>
    elapsed.TotalSeconds >= 1 ? $"{elapsed.TotalSeconds:0.0} s" : $"{elapsed.TotalMilliseconds:0} ms";

int ReadOption(string name, int defaultValue)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? int.Parse(args[index + 1]) : defaultValue;
}
