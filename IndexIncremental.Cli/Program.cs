using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace IndexIncremental.Cli;

public static partial class Program
{
    public static void Main(string[] args)
    {
        EnumerationOptions options = new()
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.System,
        };

        Console.WriteLine($"Gathering \"{Path.GetFullPath(args[0])}\"");
        RootDirectoryItem dir1 = GetDirectory(args[0], options);

        Console.WriteLine($"Gathering \"{Path.GetFullPath(args[1])}\"");
        RootDirectoryItem dir2 = GetDirectory(args[1], options);

        int fileCount = 0;
        int dirCount = 0;

        void VisitDirectory(DirectoryItem rootDirectory)
        {
            string rootName = rootDirectory.GetFullName();
            //Console.WriteLine(rootName);

            foreach (DirectoryItem directory in rootDirectory.Directories.Values)
            {
                VisitDirectory(directory);
                dirCount++;
            }

            foreach (FileItem file in rootDirectory.Files)
            {
                fileCount++;

                string name = file.GetFullName();
                //Console.WriteLine(name);
            }
        }

        VisitDirectory(dir1);
        Console.WriteLine($"Found {fileCount} files and {dirCount} directories");

        fileCount = 0;
        dirCount = 0;

        VisitDirectory(dir2);
        Console.WriteLine($"Found {fileCount} files and {dirCount} directories");

        Console.ReadKey();
    }

    public static RootDirectoryItem GetDirectory(string directory, EnumerationOptions options)
    {
        using FileSystemItemEnumerator enumerator = new(directory, options);

        while (enumerator.MoveNext())
        {
        }

        return enumerator.Root;
    }
}
