using System.Collections.Concurrent;

namespace OneIncTask.Application.Services;

public class JobCancellationRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _cancellationTokenSources = new();

    public CancellationTokenSource Register(Guid jobId)
    {
        var cts = new CancellationTokenSource();
        _cancellationTokenSources[jobId] = cts;
        return cts;
    }

    public bool TryCancel(Guid jobId)
    {
        if (_cancellationTokenSources.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
            return true;
        }
        return false;
    }

    public void Unregister(Guid jobId)
    {
        if (_cancellationTokenSources.TryRemove(jobId, out var cts))
        {
            cts.Dispose();
        }
    }
}
