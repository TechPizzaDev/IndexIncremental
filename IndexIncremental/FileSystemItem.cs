using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace IndexIncremental;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public abstract class FileSystemItem
{
    public const char DefaultSeparator = '/';

    public string Name { get; }
    public DirectoryItem? Parent { get; }

    private DirectoryItem? Root => GetRootParent();

    public FileSystemItem(string name, DirectoryItem? parent)
    {
        Name = name;
        Parent = parent;
    }

    public DirectoryItem? GetRootParent()
    {
        DirectoryItem? dir = Parent;
        while (dir != null)
        {
            if (dir.Parent == null)
            {
                break;
            }
            dir = dir.Parent;
        }
        return dir;
    }

    public void GetDirectoryTree(Stack<DirectoryItem> itemStack)
    {
        DirectoryItem? dir = Parent;
        while (dir != null)
        {
            itemStack.Push(dir);
            if (dir.Parent == null)
            {
                break;
            }
            dir = dir.Parent;
        }
    }

    public void GetDirectoryNameTree(Stack<string> nameStack)
    {
        DirectoryItem? dir = Parent;
        while (dir != null)
        {
            nameStack.Push(dir.Name);
            if (dir.Parent == null)
            {
                return;
            }
            dir = dir.Parent;
        }
    }

    public void GetDirectoryName(StackStringBuilder stackBuilder, char separator = DefaultSeparator)
    {
        Stack<string> stack = stackBuilder.Stack;
        StringBuilder builder = stackBuilder.Builder;

        GetDirectoryNameTree(stack);

        using Stack<string>.Enumerator enumerator = stack.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return;
        }
        builder.Append(enumerator.Current);

        while (enumerator.MoveNext())
        {
            builder.Append(separator);
            builder.Append(enumerator.Current);
        }
    }

    public void GetFullName(StackStringBuilder stackBuilder, char separator = DefaultSeparator)
    {
        GetDirectoryName(stackBuilder, separator);

        stackBuilder.Builder.Append(separator);
        stackBuilder.Builder.Append(Name);
    }

    public string GetDirectoryName(char separator = DefaultSeparator)
    {
        StackStringBuilder builder = StackStringBuilderCache.Get();
        GetDirectoryName(builder, separator);
        return StackStringBuilderCache.ToStringAndClear(builder);
    }

    public string GetFullName(char separator = DefaultSeparator)
    {
        StackStringBuilder builder = StackStringBuilderCache.Get();
        GetFullName(builder, separator);
        return StackStringBuilderCache.ToStringAndClear(builder);
    }

    private string GetDebuggerDisplay()
    {
        StackStringBuilder stackBuilder = new();
        StringBuilder builder = stackBuilder.Builder;

        builder.Append(GetType().ToString());

        builder.Append('(');
        GetFullName(stackBuilder, DefaultSeparator);
        builder.Append(')');

        return builder.ToString();
    }
}
