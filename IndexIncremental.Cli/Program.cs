using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blake3;

namespace IndexIncremental.Cli;

public static partial class Program
{
    public static void Main(string[] args)
    {
        string path1 = Path.GetFullPath(args[0]);
        string path2 = Path.GetFullPath(args[1]);
        bool equalPaths = path1 == path2;

        EnumerationOptions options = new()
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.System,
        };

        Console.WriteLine($"Gathering \"{path1}\"");
        RootDirectoryItem dir1 = GetDirectory(path1, options);

        RootDirectoryItem dir2 = dir1;
        if (!equalPaths)
        {
            Console.WriteLine($"Gathering \"{path2}\"");
            dir2 = GetDirectory(path2, options);
        }

        static void VisitDirectory(DirectoryItem rootDirectory, ref int dirCount, ref int fileCount, ref ulong size)
        {
            foreach (DirectoryItem directory in rootDirectory.Directories.Values)
            {
                VisitDirectory(directory, ref dirCount, ref fileCount, ref size);
                dirCount++;
            }

            RootDirectoryItem root;
            if (rootDirectory is RootDirectoryItem currentRoot)
                root = currentRoot;
            else
                root = (RootDirectoryItem)rootDirectory.GetRootParent()!;

            foreach (FileItem file in rootDirectory.Files)
            {
                fileCount++;

                FileInfo info = new(Path.Join(root.OriginalName, file.GetFullName()));
                size += (ulong)info.Length;
            }
        }

        int fileCount1 = 0;
        int dirCount1 = 0;
        ulong totalSize1 = 0;
        VisitDirectory(dir1, ref fileCount1, ref dirCount1, ref totalSize1);
        Console.WriteLine($"Found {fileCount1} files and {dirCount1} directories ({totalSize1 / 1024 / 1024}MB): ");

        int fileCount2 = 0;
        int dirCount2 = 0;
        ulong totalSize2 = 0;
        if (!equalPaths)
        {
            VisitDirectory(dir2, ref dirCount2, ref fileCount2, ref totalSize2);
            Console.WriteLine($"Found {fileCount2} files and {dirCount2} directories ({totalSize2 / 1024 / 1024}MB): ");
        }

        List<FileSystemItem> unmoved;

        if (!equalPaths)
        {
            Console.WriteLine("Calculating diff...");
            FileSystemItemDifference difference = FileSystemItemDifference.Create(dir1, dir2, trackUnchanged: true);

            Console.WriteLine($"{difference.Added.Count} items added");
            Console.WriteLine($"{difference.Removed.Count} items removed");
            unmoved = difference.Unchanged;
        }
        else
        {
            unmoved = dir1.EnumerateFileSystemItems().ToList();
        }

        Console.WriteLine($"{unmoved.Count} items unmoved");

        Console.WriteLine("Comparing unmoved files...");

        List<FileItem> changedFiles = new();

        StackStringBuilder builder = new();

        using Hasher hasher1 = Hasher.New();
        using Hasher hasher2 = Hasher.New();

        Span<byte> buffer1 = new byte[1024 * 1024];
        Span<byte> buffer2 = new byte[1024 * 1024];

        ulong unmovedTotalSize1 = 0;
        ulong unmovedTotalSize2 = 0;

        foreach (FileSystemItem item in unmoved)
        {
            if (item is FileItem file)
            {
                builder.Builder.Append(dir1.OriginalName);
                file.GetFullName(builder, Path.DirectorySeparatorChar);
                string file1 = builder.ToStringAndClear();

                builder.Builder.Append(dir2.OriginalName);
                file.GetFullName(builder, Path.DirectorySeparatorChar);
                string file2 = builder.ToStringAndClear();

                FileInfo fileInfo1 = new(file1);
                FileInfo fileInfo2 = new(file2);

                unmovedTotalSize1 += (ulong)fileInfo1.Length;
                unmovedTotalSize2 += (ulong)fileInfo2.Length;
            }
        }

        ComparisonState state1 = new()
        {
            TotalSize = unmovedTotalSize1,
            TotalSizePart = unmovedTotalSize1 / 250,
        };

        ComparisonState state2 = new()
        {
            TotalSize = unmovedTotalSize2,
            TotalSizePart = unmovedTotalSize2 / 250,
        };

        int index = 0;
        foreach (FileSystemItem item in unmoved)
        {
            index++;
            //Console.WriteLine($"{index} / {unmoved.Count}");

            if (item is FileItem file)
            {
                builder.Builder.Append(dir1.OriginalName);
                file.GetFullName(builder, Path.DirectorySeparatorChar);
                string file1 = builder.ToStringAndClear();

                builder.Builder.Append(dir2.OriginalName);
                file.GetFullName(builder, Path.DirectorySeparatorChar);
                string file2 = builder.ToStringAndClear();

                using FileStream fs1 = new(file1, FileMode.Open, FileAccess.Read, FileShare.Read, 0);
                using FileStream fs2 = new(file2, FileMode.Open, FileAccess.Read, FileShare.Read, 0);

                if (AreEqual(ref state1, ref state2, fs1, fs2, buffer1, buffer2, hasher1, hasher2))
                {
                    continue;
                }
                changedFiles.Add(file);
            }
        }

        Console.WriteLine("Finished");
        Console.WriteLine($"{changedFiles.Count} files changed");

        Console.ReadKey();
    }

    public static bool AreEqual(
        ref ComparisonState state1,
        ref ComparisonState state2,
        FileStream fs1,
        FileStream fs2,
        Span<byte> buffer1,
        Span<byte> buffer2,
        Hasher hasher1,
        Hasher hasher2)
    {
        ulong length1 = (ulong)fs1.Length;
        ulong length2 = (ulong)fs2.Length;

        if (length1 != length2)
        {
            state1.TotalCompared += length1;
            state2.TotalCompared += length2;

            return false;
        }

        hasher1.Reset();
        hasher2.Reset();

        ulong thisRead1 = 0;
        ulong thisRead2 = 0;

        int read1;
        int read2;
        do
        {
            read1 = fs1.Read(buffer1);
            read2 = fs2.Read(buffer2);

            thisRead1 += (ulong)read1;
            thisRead2 += (ulong)read2;

            if (read1 == 0 || read2 == 0)
            {
                break;
            }

            if (read1 != read2)
            {
                break;
            }

            hasher1.Update(buffer1.Slice(0, read1));
            hasher2.Update(buffer2.Slice(0, read2));

            ulong actualRead1 = (state1.TotalCompared + thisRead1);
            if (actualRead1 - state1.LastCompared >= state1.TotalSizePart)
            {
                Console.WriteLine($"{(double)actualRead1 / state1.TotalSize * 100:0.0}%");
                state1.LastCompared = actualRead1;
            }

            Hash hash1 = hasher1.Finalize();
            Hash hash2 = hasher2.Finalize();
            if (hash1.AsSpanUnsafe().SequenceEqual(hash2.AsSpanUnsafe()))
            {
                continue;
            }
        }
        while (true);

        state1.TotalRead += thisRead1;
        state2.TotalRead += thisRead2;

        state1.TotalCompared += length1;
        state2.TotalCompared += length2;

        if (read1 == 0 && read2 == 0)
        {
            Hash hash1 = hasher1.Finalize();
            Hash hash2 = hasher2.Finalize();
            if (hash1.AsSpanUnsafe().SequenceEqual(hash2.AsSpanUnsafe()))
            {
                return true;
            }
        }

        return false;
    }

    public static RootDirectoryItem GetDirectory(string directory, EnumerationOptions options)
    {
        using FileSystemItemEnumerator enumerator = new(directory, options);

        while (enumerator.MoveNext())
        {
        }

        return enumerator.Root;
    }

    public struct ComparisonState
    {
        public ulong TotalSize;
        
        public ulong TotalSizePart;
        
        public ulong TotalRead;
        
        public ulong TotalCompared;
        
        public ulong LastCompared;
    }
}
