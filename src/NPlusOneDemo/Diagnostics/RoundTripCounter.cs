using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NPlusOneDemo.Diagnostics;

/// <summary>
/// Counts database commands, rows read and HTTP calls, and adds a simulated network
/// round trip to each database command.
/// </summary>
public sealed class RoundTripCounter : DbCommandInterceptor
{
    private int _dbQueries;
    private long _rowsRead;
    private int _httpCalls;

    public TimeSpan DbLatency { get; set; }

    public int DbQueries => _dbQueries;
    public long RowsRead => _rowsRead;
    public int HttpCalls => _httpCalls;

    public void Reset()
    {
        _dbQueries = 0;
        _rowsRead = 0;
        _httpCalls = 0;
    }

    public void OnHttpCall() => Interlocked.Increment(ref _httpCalls);

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        OnDbCommand();
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        OnDbCommand();
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        OnDbCommand();
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        OnDbCommand();
        return ValueTask.FromResult(result);
    }

    // EF's own ReadCount misses the readers of split queries, so the rows are counted by a wrapper.
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result) =>
        new CountingDataReader(result, OnRow);

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<DbDataReader>(new CountingDataReader(result, OnRow));

    private void OnRow() => Interlocked.Increment(ref _rowsRead);

    private void OnDbCommand()
    {
        Interlocked.Increment(ref _dbQueries);
        SimulatedNetwork.Wait(DbLatency);
    }
}
