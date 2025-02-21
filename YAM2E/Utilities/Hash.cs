using System;
using System.IO;
using System.Security.Cryptography;

namespace LAMP.Utilities;

/// <summary>
/// Utility class for hashing operations
/// </summary>
public static class Hash
{
    /// <summary>
    /// The MD5 Hash of the US version of Metroid 2
    /// </summary>
    public const string Metroid2US = "9639948ad274fa15281f549e5f9c4d87";

    /// <summary>
    /// Creates a MD5 hash of the input file
    /// </summary>
    /// <param name="FileName">Path to the file</param>
    /// <returns>MD5 Hash as a string</returns>
    public static string GetHash(string FileName)
    {
        try
        {
            using (MD5 hasher = MD5.Create())
            using (FileStream stream = File.OpenRead(FileName))
            {
                byte[] hash = hasher.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    /// <summary>
    /// Compares the MD5 hash of a file to another MD5 hash
    /// </summary>
    /// <param name="FileName">Path to the file</param>
    /// <param name="Hash">MD5 Hash to compare to</param>
    /// <returns>true if MD5 Hash of file is the same as the <paramref name="Hash"/>, false if not or file hash is null</returns>
    public static bool Compare(string FileName, string Hash)
    {
        string h = GetHash(FileName);
        if (h == null) return false;
        return h == Hash;
    }

    /// <summary>
    /// Compares the MD5 hash of two files
    /// </summary>
    /// <returns>true if MD5 hashes of both files match, false if not or if either hash is null</returns>
    public static bool CompareFiles(string FileName1, string FileName2)
    {
        string hash1 = GetHash(FileName1);
        string hash2 = GetHash(FileName2);
        if (hash1 == null || hash2 == null) return false;
        return hash1 == hash2;
    }
}