using System;
using System.IO;
using System.IO.Enumeration;

namespace IndexIncremental;

public sealed class FileSystemItemEnumerator : FileSystemEnumerator<FileSystemItem>
{
    public RootDirectoryItem Root { get; }

    public FileSystemItemEnumerator(string directory, EnumerationOptions? options = null) : base(directory, options)
    {
        Root = new RootDirectoryItem(directory, "", null);
    }

    protected override FileSystemItem TransformEntry(ref FileSystemEntry entry)
    {
        ReadOnlySpan<char> directoryName = entry.Directory.Slice(entry.RootDirectory.Length);
        DirectoryItem directory = GetTopDirectory(directoryName);

        string name = entry.FileName.ToString();

        if (entry.IsDirectory)
        {
            DirectoryItem item = new(name, directory);
            directory.Directories.Add(name, item);
            return item;
        }
        else
        {
            FileItem item = new(name, directory);
            directory.Files.Add(item);
            return item;
        }
    }

    private DirectoryItem GetTopDirectory(ReadOnlySpan<char> directory)
    {
        DirectoryItem currentDir = Root;
        if (directory.IsEmpty)
        {
            return currentDir;
        }

        if (directory[0] == Path.DirectorySeparatorChar)
        {
            directory = directory.Slice(1);
        }

        int index = 0;
        do
        {
            int nextIndex = directory.IndexOf(Path.DirectorySeparatorChar);
            if (nextIndex == -1)
            {
                break;
            }

            ReadOnlySpan<char> name = directory[index..nextIndex];
            currentDir = currentDir.Directories[name.ToString()];

            directory = directory.Slice(nextIndex + 1);
        }
        while (true);

        return currentDir.Directories[directory.ToString()];
    }
}
