using System;
using System.IO;
using System.Text;
using GameFramework.Core.Validation;
using UnityEngine;

namespace GameFramework.Runtime.Persistence
{
    /// <summary>
    /// Default production storage: one file per key under a "Saves" subfolder of
    /// <see cref="Application.persistentDataPath"/> (the platform-appropriate writable location on
    /// both Android and iOS — never a hard-coded path).
    ///
    /// <para><b>Atomic write (Phase 19):</b></para>
    /// <list type="number">
    /// <item>Write the new contents to <c>"{file}.tmp"</c> and flush it to the device
    /// (<see cref="FileStream.Flush(bool)"/> with <c>flushToDisk: true</c>).</item>
    /// <item>Read the temp file back and compare it to what was meant to be written - a short or
    /// mangled write is rejected before it can replace anything.</item>
    /// <item>Swap it into place with <see cref="File.Replace(string,string,string)"/>, which is a
    /// single atomic rename on POSIX filesystems (Android/iOS/macOS) and <c>ReplaceFile</c> on
    /// Windows. At no point is the primary file partially written.</item>
    /// <item>If <see cref="File.Replace(string,string,string)"/> is unavailable or refuses on this
    /// platform/filesystem, fall back to rename-aside: primary → <c>"{file}.old"</c>, temp → primary,
    /// delete <c>.old</c>. A crash between the two renames leaves only <c>.old</c>, which
    /// <see cref="Exists"/>/<see cref="ReadText"/> restore on next access - so the previous good copy
    /// is never lost.</item>
    /// </list>
    ///
    /// <para>Any failure throws (after cleaning up the temp file) and leaves the previous primary
    /// file untouched - the caller decides how to report it. Keys are validated to contain no path
    /// separators or characters that are invalid in a file name on any supported platform, so a key
    /// can never address a file outside the save folder.</para>
    ///
    /// <para><b>Platform notes:</b> iOS/Android app sandboxes allow rename within the app's own
    /// persistent data directory; neither guarantees durability against a sudden power loss beyond
    /// what the flush provides (the OS/flash controller may still reorder writes). The atomic swap
    /// protects against the realistic mobile failure - the process being killed mid-save - not
    /// against hardware failure.</para>
    /// </summary>
    public sealed class FilePersistenceStorage : IPersistenceStorage
    {
        private const string FileExtension = ".sav";
        private const string TempSuffix = ".tmp";
        private const string PreviousSuffix = ".old";

        /// <summary>Longest accepted key. Keeps "{key}.sav.tmp"/".old" safely under the common
        /// 255-byte file-name limit on every supported filesystem.</summary>
        public const int MaxKeyLength = 200;

        // The union of characters Windows forbids in a file name plus both path separators - fixed
        // rather than Path.GetInvalidFileNameChars(), which differs per OS (on Android/iOS only '/' and
        // '\0'), so a key accepted in the Editor is always accepted on device and vice versa.
        private static readonly char[] InvalidKeyChars = { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly string _rootDirectory;

        public FilePersistenceStorage() : this(Path.Combine(Application.persistentDataPath, "Saves"))
        {
        }

        public FilePersistenceStorage(string rootDirectory)
        {
            _rootDirectory = Guard.NotNullOrEmpty(rootDirectory, nameof(rootDirectory));
        }

        public bool Exists(string key)
        {
            string path = GetPath(key);
            RecoverInterruptedReplace(path);
            return File.Exists(path);
        }

        public string ReadText(string key)
        {
            string path = GetPath(key);
            RecoverInterruptedReplace(path);
            return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;
        }

        public void WriteText(string key, string contents)
        {
            string path = GetPath(key);
            Directory.CreateDirectory(_rootDirectory);
            RecoverInterruptedReplace(path);

            string tempPath = path + TempSuffix;
            try
            {
                WriteAndFlush(tempPath, contents ?? string.Empty);
                VerifyWritten(tempPath, contents ?? string.Empty);
            }
            catch
            {
                TryDelete(tempPath);
                throw;
            }

            if (!File.Exists(path))
            {
                File.Move(tempPath, path);
                return;
            }

            try
            {
                File.Replace(tempPath, path, null);
            }
            catch (Exception exception) when (exception is PlatformNotSupportedException || exception is IOException)
            {
                // Some platform/filesystem combinations refuse Replace; the primary is still intact
                // here (Replace either fully happened or didn't), so fall back to rename-aside.
                ReplaceViaRename(tempPath, path);
            }
        }

        public void Delete(string key)
        {
            string path = GetPath(key);
            DeleteIfExists(path);
            DeleteIfExists(path + TempSuffix);
            DeleteIfExists(path + PreviousSuffix);
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> if <paramref name="key"/> could not safely be used
        /// as a file name on every supported platform. Public so other storage implementations (and
        /// tests) can apply the identical rule.
        /// </summary>
        public static void ValidateKey(string key)
        {
            Guard.NotNullOrEmpty(key, nameof(key));

            if (key.Length > MaxKeyLength)
            {
                throw new ArgumentException($"Persistence key is {key.Length} characters; the maximum is {MaxKeyLength}.", nameof(key));
            }

            if (key == "." || key == "..")
            {
                throw new ArgumentException($"Persistence key '{key}' is not a valid file name.", nameof(key));
            }

            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (c < 32 || Array.IndexOf(InvalidKeyChars, c) >= 0)
                {
                    throw new ArgumentException(
                        $"Persistence key '{key}' contains a character (code {(int)c}) that is not allowed in a storage key.", nameof(key));
                }
            }
        }

        private static void WriteAndFlush(string path, string contents)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = Utf8NoBom.GetBytes(contents);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }
        }

        private static void VerifyWritten(string path, string expected)
        {
            string actual = File.ReadAllText(path, Encoding.UTF8);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                throw new IOException($"Verification of '{Path.GetFileName(path)}' failed: written data does not match (short or corrupted write).");
            }
        }

        private static void ReplaceViaRename(string tempPath, string path)
        {
            string previousPath = path + PreviousSuffix;
            DeleteIfExists(previousPath);
            File.Move(path, previousPath);
            File.Move(tempPath, path);
            DeleteIfExists(previousPath);
        }

        /// <summary>Completes/undoes a rename-aside replace that was interrupted by a crash: if the
        /// primary is missing but <c>.old</c> survived, <c>.old</c> is the last good copy and is
        /// restored; if both exist, the swap had finished and <c>.old</c> is just stale.</summary>
        private static void RecoverInterruptedReplace(string path)
        {
            string previousPath = path + PreviousSuffix;
            if (!File.Exists(previousPath))
            {
                return;
            }

            if (File.Exists(path))
            {
                TryDelete(previousPath);
            }
            else
            {
                File.Move(previousPath, path);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                DeleteIfExists(path);
            }
            catch (IOException)
            {
                // Cleanup of a temp/stale file only - the next write overwrites/retries it anyway, and
                // the operation that triggered the cleanup is already reporting its own outcome.
            }
            catch (UnauthorizedAccessException)
            {
                // Same reasoning as above.
            }
        }

        private string GetPath(string key)
        {
            ValidateKey(key);
            return Path.Combine(_rootDirectory, key + FileExtension);
        }
    }
}
