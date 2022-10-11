using System;
using System.Diagnostics;

namespace IndexIncremental;

[DebuggerDisplay("{Item,nq}")]
public readonly struct FileSystemItemByName : IEquatable<FileSystemItemByName>
{
    private readonly int _hashCode;

    public FileSystemItem Item { get; }

    public FileSystemItemByName(FileSystemItem item)
    {
        Item = item;
        _hashCode = BakeHashCode(item);
    }

    public bool Equals(FileSystemItemByName other)
    {
        FileSystemItem? itemX = Item;
        FileSystemItem? itemY = other.Item;

        do
        {
            if (itemX == null || itemY == null)
            {
                break;
            }

            if (itemX.Name != itemY.Name)
            {
                return false;
            }

            itemX = itemX.Parent;
            itemY = itemY.Parent;
        }
        while (true);

        return itemX == null && itemY == null;
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }

    private static int BakeHashCode(FileSystemItem obj)
    {
        HashCode hash = new();

        FileSystemItem? item = obj;
        do
        {
            if (item == null)
            {
                break;
            }

            hash.Add(item.Name.GetHashCode());
            item = item.Parent;
        }
        while (item != null);

        return hash.ToHashCode();
    }
}