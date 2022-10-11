using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexIncremental;

public class FileSystemItemDifference
{
    public List<FileSystemItem> Added { get; } = new();
    public List<FileSystemItem> Removed { get; } = new();

    public static FileSystemItemDifference Create(
        RootDirectoryItem rootDir1,
        RootDirectoryItem rootDir2)
    {
        FileSystemItemDifference difference = new();

        List<FileSystemItemByName> list1 = new(rootDir1.EnumerateFileSystemItems().Select(x => new FileSystemItemByName(x)));
        List<FileSystemItemByName> list2 = new(rootDir2.EnumerateFileSystemItems().Select(x => new FileSystemItemByName(x)));

        HashSet<FileSystemItemByName> set1 = new(list1);
        HashSet<FileSystemItemByName> set2 = new(list2);

        set2.SymmetricExceptWith(set1);

        foreach (FileSystemItemByName item in set2)
        {
            DirectoryItem? parent = item.Item.GetRootParent();
            if (parent == rootDir1)
            {
                difference.Removed.Add(item.Item);
            }
            else if (parent == rootDir2)
            {
                difference.Added.Add(item.Item);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        return difference;
    }
}