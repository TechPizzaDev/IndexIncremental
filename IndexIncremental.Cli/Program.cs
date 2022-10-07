using System.IO.Enumeration;

namespace IndexIncremental.Cli;

public static class Program
{
    public static void Main(string[] args)
    {
        FileEnumerator enumerator = new("C:/Tmp/diffs/ComputeMesh", new EnumerationOptions()
        {
            RecurseSubdirectories = true,
        });

        while (enumerator.MoveNext())
        {
            var entry = enumerator.Current;
        }

        Console.ReadKey();
    }
}

public sealed class FileEnumerator : FileSystemEnumerator<int>
{
    public FileEnumerator(string directory, EnumerationOptions? options = null) : base(directory, options)
    {
    }

    protected override int TransformEntry(ref FileSystemEntry entry)
    {
        Console.WriteLine(entry.ToFullPath());

        return default;
    }
}