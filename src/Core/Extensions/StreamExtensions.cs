using System;
using System.IO;
using System.Text;

namespace CoApp.VsExtension
{
    public static class StreamExtensions
    {
        public static byte[] ReadAllBytes(this Stream stream)
        {
            var memoryStream = stream as MemoryStream;
            if (memoryStream != null)
            {
                return memoryStream.ToArray();
            }
            else
            {
                using (memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        /// <summary>
        /// Turns an existing stream into one that a stream factory that can be reopened.
        /// </summary>        
        public static Func<Stream> ToStreamFactory(this Stream stream)
        {
            byte[] buffer;

            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                buffer = ms.ToArray();
            }

            return () => new MemoryStream(buffer);
        }

        public static string ReadToEnd(this Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            {
                return streamReader.ReadToEnd();
            }
        }

        public static Stream AsStream(this string value)
        {
            return AsStream(value, Encoding.UTF8);
        }

        public static Stream AsStream(this string value, Encoding encoding)
        {
            return new MemoryStream(encoding.GetBytes(value));
        }

    }
}
