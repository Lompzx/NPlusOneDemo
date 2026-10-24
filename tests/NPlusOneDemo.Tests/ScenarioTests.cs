using NPlusOneDemo.Scenarios;
using NPlusOneDemo.Shop;

namespace NPlusOneDemo.Tests;

/// <summary>
/// An N+1 shows up as round trips that grow with the data. These tests run each version
/// against two data sizes: the naive one must grow, the fixed one must stay the same.
/// The same check works as a regression test for real endpoints.
/// </summary>
public class ScenarioTests
{
    [Theory]
    [InlineData(typeof(LazyLoadingInLoop))]
    [InlineData(typeof(RepositoryInLoop))]
    [InlineData(typeof(ReturningEntities))]
    [InlineData(typeof(MappingAfterToList))]
    [InlineData(typeof(HttpCallsInLoop))]
    public async Task Naive_version_grows_with_the_data_and_fixed_version_does_not(Type scenarioType)
    {
        var scenario = (IScenario)Activator.CreateInstance(scenarioType)!;

        var small = await MeasureAsync(scenario, orders: 10);
        var large = await MeasureAsync(scenario, orders: 20);

        Assert.True(large.Naive.RoundTrips > small.Naive.RoundTrips,
            $"naive: {small.Naive.RoundTrips} -> {large.Naive.RoundTrips} round trips");
        Assert.Equal(small.Fixed.RoundTrips, large.Fixed.RoundTrips);
    }

    [Fact]
    public async Task Split_query_reads_fewer_rows_than_including_everything()
    {
        var (naive, fixedRun) = await MeasureAsync(new IncludeEverything(), orders: 20);

        Assert.Equal(1, naive.DbQueries);
        Assert.Equal(3, fixedRun.DbQueries);
        Assert.True(fixedRun.RowsRead < naive.RowsRead, $"{fixedRun.RowsRead} vs {naive.RowsRead} rows");
    }

    private static async Task<(RunResult Naive, RunResult Fixed)> MeasureAsync(IScenario scenario, int orders)
    {
        await using var shop = await DemoShop.CreateAsync(orders, TimeSpan.Zero, TimeSpan.Zero);
        return (await shop.MeasureAsync(scenario.RunNaiveAsync), await shop.MeasureAsync(scenario.RunFixedAsync));
    }
}
