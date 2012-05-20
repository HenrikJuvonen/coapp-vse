using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using CoGet.Resources;

namespace CoGet
{
    public static class FileSystemExtensions
    {
        public static IEnumerable<string> GetFiles(this IFileSystem fileSystem, string path, string filter)
        {
            return fileSystem.GetFiles(path, filter, recursive: false);
        }

        internal static IEnumerable<string> GetFilesSafe(this IFileSystem fileSystem, string path)
        {
            return GetFilesSafe(fileSystem, path, "*.*");
        }

        internal static IEnumerable<string> GetFilesSafe(this IFileSystem fileSystem, string path, string filter)
        {
            try
            {
                return fileSystem.GetFiles(path, filter);
            }
            catch (Exception e)
            {
                fileSystem.Logger.Log(MessageLevel.Warning, e.Message);
            }

            return Enumerable.Empty<string>();
        }

        internal static void DeleteDirectorySafe(this IFileSystem fileSystem, string path, bool recursive)
        {
            DoSafeAction(() => fileSystem.DeleteDirectory(path, recursive), fileSystem.Logger);
        }

        internal static void DeleteFileSafe(this IFileSystem fileSystem, string path)
        {
            DoSafeAction(() => fileSystem.DeleteFile(path), fileSystem.Logger);
        }

        internal static void AddFileWithCheck(this IFileSystem fileSystem, string path, Func<Stream> streamFactory)
        {
            if (fileSystem.FileExists(path))
            {
                fileSystem.Logger.Log(MessageLevel.Warning, CoGetResources.Warning_FileAlreadyExists, path);
            }
            else
            {
                using (Stream stream = streamFactory())
                {
                    fileSystem.AddFile(path, stream);
                }
            }
        }

        internal static void AddFileWithCheck(this IFileSystem fileSystem, string path, Action<Stream> write)
        {
            fileSystem.AddFileWithCheck(path, () =>
            {
                var stream = new MemoryStream();
                write(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            });
        }

        internal static void AddFile(this IFileSystem fileSystem, string path, Action<Stream> write)
        {
            using (var stream = new MemoryStream())
            {
                write(stream);
                stream.Seek(0, SeekOrigin.Begin);
                fileSystem.AddFile(path, stream);
            }
        }

        internal static IEnumerable<string> GetDirectories(string path)
        {
            foreach (var index in IndexOfAll(path, Path.DirectorySeparatorChar))
            {
                yield return path.Substring(0, index);
            }
            yield return path;
        }

        private static IEnumerable<int> IndexOfAll(string value, char ch)
        {
            int index = -1;
            do
            {
                index = value.IndexOf(ch, index + 1);
                if (index >= 0)
                {
                    yield return index;
                }
            }
            while (index >= 0);
        }

        private static void DoSafeAction(Action action, ILogger logger)
        {
            try
            {
                Attempt(action);
            }
            catch (Exception e)
            {
                logger.Log(MessageLevel.Warning, e.Message);
            }
        }

        private static void Attempt(Action action, int retries = 3, int delayBeforeRetry = 150)
        {
            while (retries > 0)
            {
                try
                {
                    action();
                    break;
                }
                catch
                {
                    retries--;
                    if (retries == 0)
                    {
                        throw;
                    }
                }
                Thread.Sleep(delayBeforeRetry);
            }
        }
    }
}