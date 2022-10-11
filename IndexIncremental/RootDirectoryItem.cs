using System.Diagnostics;
using System.Text;

namespace IndexIncremental;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class RootDirectoryItem : DirectoryItem
{
    public string OriginalName { get; }

    public RootDirectoryItem(string originalPath, string name, DirectoryItem? parent) : base(name, parent)
    {
        OriginalName = originalPath;
    }

    public string GetDebuggerDisplay()
    {
        StackStringBuilder stackBuilder = new();
        StringBuilder builder = stackBuilder.Builder;

        builder.Append(GetType().ToString());

        builder.Append('(');
        builder.Append(OriginalName);
        GetFullName(stackBuilder, DefaultSeparator);
        builder.Append(')');

        return builder.ToString();
    }
}