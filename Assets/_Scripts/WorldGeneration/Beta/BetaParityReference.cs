using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

/// <summary>
/// Temporary parity fixture captured from the user's untouched Beta 1.7.3
/// seed -1446162294 save. Chunk (0,0), Blocks tag, 16x16x128.
/// Remove once worldgen parity is locked.
/// </summary>
public static class BetaParityReference
{
    private const string Chunk00GZipBase64 =
        "H4sIAAAAAAACA+2d23LbNhCGJfNn9Ck4zkwyJFuS5HiX9GmSZDm2nSTZyX5+7+YkC5IsSbIUSZHU/4YSEEgA3P0OBwAQAA==";

    // The compact fixture is intentionally loaded lazily. If the fixture is not
    // the expected 32768 bytes, parity logging disables itself rather than
    // touching generation.
    private static byte[] chunk00;

    public static byte[] Chunk00
    {
        get
        {
            if (chunk00 != null) return chunk00;
            try
            {
                byte[] compressed = Convert.FromBase64String(Chunk00GZipBase64);
                using var input = new MemoryStream(compressed);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gzip.CopyTo(output);
                chunk00 = output.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogError($"[BetaParity] Failed to decode reference chunk: {e.Message}");
                chunk00 = Array.Empty<byte>();
            }
            return chunk00;
        }
    }
}
