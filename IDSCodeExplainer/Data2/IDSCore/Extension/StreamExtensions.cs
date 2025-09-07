using System;
using System.IO;
using System.Threading;

namespace IDSCore.Extension
{
    public static class StreamExtensions
    {
        public static void CopyTo(this Stream source, Stream destination, int bufferSize, long totalSize,
            IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!source.CanRead)
            {
                throw new ArgumentException("Has to be readable", nameof(source));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (!destination.CanWrite)
            {
                throw new ArgumentException("Has to be writable", nameof(destination));
            }

            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            var buffer = new byte[bufferSize];
            var totalBytesRead = 0;
            int bytesRead;
            while (!cancellationToken.IsCancellationRequested && 
                   (bytesRead = source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).Result) != 0)
            {
                destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).Wait();
                totalBytesRead += bytesRead;
                var progressPercentage = (double)totalBytesRead / (double)totalSize * 100;
                progress?.Report(progressPercentage);
            }
        }
    }
}
