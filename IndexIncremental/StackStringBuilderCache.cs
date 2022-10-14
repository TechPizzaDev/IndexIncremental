using System;
using System.Runtime.CompilerServices;

namespace IndexIncremental;

public static class StackStringBuilderCache
{
    [ThreadStatic]
    private static WeakReference<StackStringBuilder>? _cache;

    [MethodImpl(MethodImplOptions.NoInlining)] // do not inline into Get to be as small as possible
    private static WeakReference<StackStringBuilder> CreateCache()
    {
        _cache = new WeakReference<StackStringBuilder>(null!);
        return _cache;
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // do not inline into Get to be as small as possible
    private static StackStringBuilder CreateBuilder(WeakReference<StackStringBuilder> cache)
    {
        StackStringBuilder builder = new();
        cache.SetTarget(builder);
        return builder;
    }

    public static StackStringBuilder Get()
    {
        WeakReference<StackStringBuilder>? cache = _cache ?? CreateCache();

        if (!cache.TryGetTarget(out StackStringBuilder? target))
        {
            target = CreateBuilder(cache);
        }
        return target;
    }

    public static string ToStringAndClear(StackStringBuilder builder)
    {
        return builder.ToStringAndClear();
    }
}
