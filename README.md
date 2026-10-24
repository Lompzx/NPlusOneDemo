# N+1 Demo

Six ways the N+1 problem sneaks into .NET code, from the textbook case to the ones that catch experienced developers, each with a fixed version next to it and measured side by side.

An N+1 happens when code loads a list (1 query) and then runs one more query for each item (N queries). With 10 rows in the development database nobody notices. With 2,000 rows in production, it's 2,001 round trips.

## Results

`dotnet run --project src/NPlusOneDemo -c Release` on 2,000 orders:

```
Scenario                                             Round trips    Rows read       Time
----------------------------------------------------------------------------------------
1. Lazy loading in a loop                    naive         2,001       22,872      2.8 s
                                             fixed             1        2,000      88 ms

2. Repository call inside a loop             naive         2,001        4,000      2.2 s
                                             fixed             2        2,200      24 ms

3. Returning entities from the API           naive           324        1,645     478 ms
                                             fixed             3        1,522      49 ms

4. Mapping after ToList                      naive           274        1,595     329 ms
                                             fixed             1          100       9 ms

5. HTTP calls inside a loop                  naive           235          234      5.9 s
                                             fixed             2          234      35 ms

6. Include everything (cartesian explosion)  naive             1        6,503      81 ms
                                             fixed             3        2,938      32 ms
```

The database is SQLite in memory, so a simulated round trip is added to each command: 1 ms for the database and 25 ms for HTTP, close to a database and a service in the same region. The round trips and rows are exact; the times depend on the machine and on those settings.

## Scenarios

| # | Scenario | Problem | Fix |
|---|---|---|---|
| 1 | [Lazy loading in a loop](src/NPlusOneDemo/Scenarios/LazyLoadingInLoop.cs) | `order.Items` inside a `foreach`, with lazy loading on | Aggregate in the query with `Select` |
| 2 | [Repository call inside a loop](src/NPlusOneDemo/Scenarios/RepositoryInLoop.cs) | `GetByIdAsync` for each order; the loop and the query live in different layers | Load in one batch (`Contains` becomes `WHERE IN`) and look up in a `Dictionary` |
| 3 | [Returning entities from the API](src/NPlusOneDemo/Scenarios/ReturningEntities.cs) | The serializer walks every navigation and lazy loading fetches each one | Project into response DTOs |
| 4 | [Mapping after ToList](src/NPlusOneDemo/Scenarios/MappingAfterToList.cs) | The mapping reads navigations after the data is in memory | Apply the same mapping inside the query (what AutoMapper's `ProjectTo` does) |
| 5 | [HTTP calls inside a loop](src/NPlusOneDemo/Scenarios/HttpCallsInLoop.cs) | One call to a catalog API per order item | One batch call (`/products?ids=...`) |
| 6 | [Include everything](src/NPlusOneDemo/Scenarios/IncludeEverything.cs) | Fixing the N+1 by including every collection: items and payments are joined, so each order comes back items × payments times | `AsSplitQuery()` or a projection |

Scenario 6 is the one that catches people who already know about N+1: a single query is not automatically better. The split query makes two more round trips but reads less than half the rows.

## Catching it

- **Look at the SQL.** Turn on EF Core command logging (`LogTo`, or the `Microsoft.EntityFrameworkCore.Database.Command` category) and count the queries of one request.
- **Measure per request.** MiniProfiler, or the dependency count per request in Application Insights or OpenTelemetry.
- **Test with realistic volumes**, not five rows.
- **Write a test for it.** [ScenarioTests](tests/NPlusOneDemo.Tests/ScenarioTests.cs) runs each version against two data sizes and checks that the round trips of the fixed version don't grow with the data. The counting is done by a [`DbCommandInterceptor`](src/NPlusOneDemo/Diagnostics/RoundTripCounter.cs), which can be reused in integration tests of a real API.
- **Avoid lazy loading.** With it, an N+1 is invisible in the code that causes it.

## Running

Requires the .NET 10 SDK.

```powershell
dotnet run --project src/NPlusOneDemo -c Release
dotnet run --project src/NPlusOneDemo -c Release -- --orders 5000 --db-latency-ms 2 --http-latency-ms 50
dotnet test
```

## Repository layout

```
├── src/NPlusOneDemo/
│   ├── Scenarios/      one file per scenario, naive and fixed versions side by side
│   ├── Shop/           entities, DbContext, seed data and a fake catalog API
│   ├── Diagnostics/    round trip and row counting, simulated latency
│   └── Program.cs      runs every scenario and prints the table
├── tests/NPlusOneDemo.Tests/
└── NPlusOneDemo.slnx
```

## License

[MIT](LICENSE)
