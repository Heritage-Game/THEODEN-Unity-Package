using System;
using System.IO;
using System.Linq;

public static class FileUtilities
{
    public static string[] ReadCompleteFile(string filePath)
    {
        return !File.Exists(filePath) ? Array.Empty<string>() : File.ReadAllLines(filePath);
    }

    public static byte[] ReadCompleteFileBytes(string filePath)
    {
        return !File.Exists(filePath) ? Array.Empty<byte>() : File.ReadAllBytes(filePath);
    }

    public static void WriteFile(string filePath, string content)
    {
        var directoryList = filePath.Split('/').ToList();
        directoryList.RemoveAt(directoryList.Count - 1);
        if (!Directory.Exists(string.Join("/", directoryList)))
            Directory.CreateDirectory(string.Join("/", directoryList));
        if (File.Exists(filePath)) File.Delete(filePath);
        File.WriteAllText(filePath, content.Trim());
    }

    public static void WriteFile(string filePath, byte[] content)
    {
        var directoryList = filePath.Split('/').ToList();
        directoryList.RemoveAt(directoryList.Count - 1);
        if (!Directory.Exists(string.Join("/", directoryList)))
            Directory.CreateDirectory(string.Join("/", directoryList));
        File.WriteAllBytes(filePath, content);
    }
}