using NPlusOneDemo.Shop;

namespace NPlusOneDemo.Scenarios;

public interface IScenario
{
    string Title { get; }

    /// <summary>The code as it is usually written, with the problem.</summary>
    Task RunNaiveAsync(DemoShop shop);

    /// <summary>The same work, fixed.</summary>
    Task RunFixedAsync(DemoShop shop);

    public static IReadOnlyList<IScenario> All { get; } =
    [
        new LazyLoadingInLoop(),
        new RepositoryInLoop(),
        new ReturningEntities(),
        new MappingAfterToList(),
        new HttpCallsInLoop(),
        new IncludeEverything(),
    ];
}
