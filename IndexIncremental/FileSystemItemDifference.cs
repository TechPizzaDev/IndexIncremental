using System;
using System.Collections.Generic;
using System.Linq;

namespace IndexIncremental;

public class FileSystemItemDifference
{
    public List<FileSystemItem> Added { get; }
    public List<FileSystemItem> Removed { get; }
    public List<FileSystemItem> Unchanged { get; }
    public bool TrackUnchanged { get; }

    private FileSystemItemDifference(bool trackUnchanged)
    {
        Added = new List<FileSystemItem>();
        Removed = new List<FileSystemItem>();
        Unchanged = new List<FileSystemItem>();
        TrackUnchanged = trackUnchanged;
    }

    public static FileSystemItemDifference Create(
        RootDirectoryItem rootDir1,
        RootDirectoryItem rootDir2,
        bool trackUnchanged)
    {
        FileSystemItemDifference difference = new(trackUnchanged);

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

        if (trackUnchanged)
        {
            set1.ExceptWith(set2);

            difference.Unchanged.EnsureCapacity(set1.Count);

            foreach (FileSystemItemByName item in set1)
            {
                difference.Unchanged.Add(item.Item);
            }
        }

        return difference;
    }
}