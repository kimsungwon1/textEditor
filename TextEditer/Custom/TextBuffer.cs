using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace TextEditer
{
    public class ITextBuffer : IDisposable
    {
        private MemoryMappedFile m_memoryMappedFile;
        private MemoryMappedViewAccessor m_memoryMappedViewAccessor;
        private readonly List<long> m_listLineOffsets = new List<long>();
        private long m_dwFileLength;

        public int LineCount => m_listLineOffsets.Count;

        public void Load(string path)
        {
            Dispose();

            FileInfo info = new FileInfo(path);
            m_dwFileLength = info.Length;

            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            m_memoryMappedFile = MemoryMappedFile.CreateFromFile(fs, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);

            m_memoryMappedViewAccessor = m_memoryMappedFile.CreateViewAccessor(
                0, 0, MemoryMappedFileAccess.Read);

            BuildLineIndex();
            if (m_listLineOffsets.Count == 0)
                m_listLineOffsets.Add(0);
        }

        private void BuildLineIndex()
        {
            if (m_memoryMappedViewAccessor == null)
                throw new InvalidOperationException("Accessor is null. Call Load() first.");

            m_listLineOffsets.Clear();
            m_listLineOffsets.Add(0);

            // 파일 전체를 훑어 '\n' 위치를 기록 (줄 시작 오프셋)
            for (long i = 0; i < m_dwFileLength; i++)
            {
                byte b = m_memoryMappedViewAccessor.ReadByte(i);
                if (b == (byte)'\n')
                {
                    long next = i + 1;
                    if (next < m_dwFileLength)
                        m_listLineOffsets.Add(next);
                }
            }
        }

        public string GetLine(int index)
        {
            if (m_memoryMappedViewAccessor == null)
                return string.Empty;

            if (index < 0 || index >= m_listLineOffsets.Count)
                return string.Empty;

            long start = m_listLineOffsets[index];
            long end = (index + 1 < m_listLineOffsets.Count) ? m_listLineOffsets[index + 1] - 1 : m_dwFileLength;

            long lenLong = end - start;
            if (lenLong <= 0)
                return string.Empty;
            
            if (lenLong > int.MaxValue)
                lenLong = int.MaxValue;

            int length = (int)lenLong;

            byte[] buffer = new byte[length];
            m_memoryMappedViewAccessor.ReadArray(start, buffer, 0, length);
            
            if (length > 0 && buffer[length - 1] == (byte)'\r')
                length--;

            return Encoding.UTF8.GetString(buffer, 0, length);
        }

        public int GetLineLength(int index)
        {
            return GetLine(index).Length;
        }

        public void Dispose()
        {
            m_memoryMappedViewAccessor?.Dispose();
            m_memoryMappedViewAccessor = null;

            m_memoryMappedFile?.Dispose();
            m_memoryMappedFile = null;

            m_listLineOffsets.Clear();
            m_dwFileLength = 0;
        }
    }
}
