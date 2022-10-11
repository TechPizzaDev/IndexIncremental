using System.Collections.Generic;

namespace IndexIncremental;

public class DirectoryItem : FileSystemItem
{
    public Dictionary<string, DirectoryItem> Directories { get; }
    public List<FileItem> Files { get; }

    public DirectoryItem(string name, DirectoryItem? parent) : base(name, parent)
    {
        Directories = new Dictionary<string, DirectoryItem>();
        Files = new List<FileItem>();
    }

    public IEnumerable<FileSystemItem> EnumerateFileSystemItems()
    {
        foreach (FileItem file in Files)
        {
            yield return file;
        }

        foreach (DirectoryItem dir in Directories.Values)
        {
            yield return dir;

            foreach (FileSystemItem item in dir.EnumerateFileSystemItems())
            {
                yield return item;
            }
        }
    }
}
