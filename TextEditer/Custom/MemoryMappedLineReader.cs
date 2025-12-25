using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditer.Custom
{
    public class MemoryMappedLineReader : IRawFileReader
    {
        private readonly MemoryMappedFile _mmf;
        private readonly MemoryMappedViewAccessor _accessor;

        public long Length { get; private set; }

        public MemoryMappedLineReader(string path)
        {
            var fi = new FileInfo(path);
            Length = fi.Length;

            _mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            _accessor = _mmf.CreateViewAccessor(0, Length, MemoryMappedFileAccess.Read);
        }

        public int ReadBytes(long fileOffset, byte[] dest, int destIndex, int count)
        {
            if (fileOffset < 0 || fileOffset > Length) throw new ArgumentOutOfRangeException(nameof(fileOffset));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (dest == null) throw new ArgumentNullException(nameof(dest));
            if (destIndex < 0 || destIndex + count > dest.Length) throw new ArgumentOutOfRangeException(nameof(destIndex));

            int toRead = (int)Math.Min(count, Length - fileOffset);
            if (toRead <= 0) return 0;

            _accessor.ReadArray(fileOffset, dest, destIndex, toRead);
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

        public int GetNextUtf8CharByteLength(long fileOffset)
        {
            if (fileOffset < 0 || fileOffset >= Length) return 0;

            byte b = _accessor.ReadByte(fileOffset);

            if ((b & 0b1000_0000) == 0) return 1;                 // 0xxxxxxx
            if ((b & 0b1110_0000) == 0b1100_0000) return 2;       // 110xxxxx
            if ((b & 0b1111_0000) == 0b1110_0000) return 3;       // 1110xxxx
            if ((b & 0b1111_1000) == 0b1111_0000) return 4;       // 11110xxx

            // 잘못된 바이트면 1로 취급
            return 1;
        }

        public int GetPrevUtf8CharByteLength(long fileOffsetMinus1)
        {
            if (fileOffsetMinus1 < 0) return 0;

            long p = fileOffsetMinus1;
            int count = 1;

            while (p > 0)
            {
                byte b = _accessor.ReadByte(p);
                if ((b & 0b1100_0000) != 0b1000_0000)
                    break;

                count++;
                p--;
                if (count >= 4) break;
            }

            return count;
        }

        public void Dispose()
        {
            _accessor.Dispose();
            _mmf.Dispose();
        }
    }
}
