using System.Collections.Concurrent;

namespace csharp_template.Registry;

public class HandlerRegistry
{
    private readonly ConcurrentDictionary<string, Type> _map = new();

    public void Register(string key, Type handlerType)
    {
        _map[key] = handlerType;
    }

    public bool TryGet(string key, out Type? handlerType)
    {
        return _map.TryGetValue(key, out handlerType);
    }
}