using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TextEditer
{
    public class cTextBuffer : IDisposable
    {
        private readonly MemoryMappedFile m_memoryMappedFile;
        private readonly MemoryMappedViewAccessor m_memoryMappedViewAccessor;

        public string m_sCurrentFilePath { get; }
        public long m_dwSnapshotSourceLength { get; private set; }
        public long m_dwOpenedFileLength { get; }
        public long m_dwLength { get; private set; }

        public cTextBuffer(string path)
        {
            m_sCurrentFilePath = path;

            FileInfo fi = new FileInfo(path);
            m_dwLength = fi.Length;

            m_dwSnapshotSourceLength = fi.Length;
            m_dwOpenedFileLength = fi.Length;

            m_memoryMappedFile = MemoryMappedFile.CreateNew(null, m_dwLength, MemoryMappedFileAccess.ReadWrite);
            m_memoryMappedViewAccessor = m_memoryMappedFile.CreateViewAccessor(0, m_dwLength, MemoryMappedFileAccess.ReadWrite);

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                CopyToSnapShot(fs);
            }
        }

        private void CopyToSnapShot(FileStream fs)
        {
            const int Chunk = 1024 * 1024;
            byte[] buf = new byte[Chunk];

            long dstOffset = 0;
            while (dstOffset < m_dwLength)
            {
                int toRead = (int)Math.Min(Chunk, m_dwLength - dstOffset);
                int read = fs.Read(buf, 0, toRead);
                if (read <= 0) break;

                m_memoryMappedViewAccessor.WriteArray(dstOffset, buf, 0, read);
                dstOffset += read;
            }
        }

        public int ReadBytes(long fileOffset, byte[] dest, int destIndex, int count)
        {
            if (fileOffset < 0 || fileOffset > m_dwLength) throw new ArgumentOutOfRangeException(nameof(fileOffset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (dest == null) throw new ArgumentNullException(nameof(dest));
            if (destIndex < 0 || destIndex + count > dest.Length) throw new ArgumentOutOfRangeException(nameof(destIndex));

            int toRead = (int)Math.Min(count, m_dwLength - fileOffset);
            if (toRead <= 0) return 0;

            m_memoryMappedViewAccessor.ReadArray(fileOffset, dest, destIndex, toRead);
            return toRead;
        }

        public string ReadUtf8(long fileOffset, int byteCount)
        {
            if (byteCount <= 0) return string.Empty;

            byte[] buf = new byte[byteCount];
            int read = ReadBytes(fileOffset, buf, 0, byteCount);
            if (read <= 0) return string.Empty;

            return Encoding.UTF8.GetString(buf, 0, read);
        }

        public void Dispose()
        {
            m_memoryMappedViewAccessor.Dispose();
            m_memoryMappedFile.Dispose();
        }
    }
}
