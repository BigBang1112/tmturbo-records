namespace TMTurboRecords;

public static class TaskExtensions
{
    public static IAsyncEnumerable<KeyValuePair<TKey, Task<TValue>>> WhenEach<TKey, TValue>(this IDictionary<TKey, Task<TValue>> tasks)
    {
        return WhenEach(tasks.ToDictionary(x => x.Value, x => x.Key));
    }

    public static async IAsyncEnumerable<KeyValuePair<TKey, Task<TValue>>> WhenEach<TKey, TValue>(this IDictionary<Task<TValue>, TKey> tasks)
    {
        tasks = new Dictionary<Task<TValue>, TKey>(tasks);

        while (tasks.Count > 0)
        {
            var task = await Task.WhenAny(tasks.Keys);
            var platform = tasks[task];
            tasks.Remove(task);

            yield return new KeyValuePair<TKey, Task<TValue>>(platform, task);
        }
    }
}
