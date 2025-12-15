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
        private FileStream m_fileStream;
        private MemoryMappedFile m_memoryMappedFile;
        private MemoryMappedViewAccessor m_memoryMappedViewAccessor;
        private readonly List<long> m_listLineOffsets;

        private readonly MemoryStream m_memoryStreamAdd = new MemoryStream(1024 * 1024);
        private readonly List<cPiece> m_listPieces;

        private long m_dwFileLength;

        // private int m_nAddPieceOffset = 0;

        public int LineCount => m_listLineOffsets.Count;

        public ITextBuffer()
        {
            m_listLineOffsets = new List<long>();
            m_listPieces = new List<cPiece>();
        }

        public void Load(string path)
        {
            Dispose();

            FileInfo info = new FileInfo(path);
            m_dwFileLength = info.Length;

            m_fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

            m_memoryMappedFile = MemoryMappedFile.CreateFromFile(m_fileStream, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, false);

            m_memoryMappedViewAccessor = m_memoryMappedFile.CreateViewAccessor(
                0, 0, MemoryMappedFileAccess.Read);

            BuildLineIndex();
            if (m_listLineOffsets.Count == 0)
                m_listLineOffsets.Add(0);

            m_listPieces.Add(new cPiece(true, 0, (int)m_dwFileLength));
        }

        public void AddBuffer(byte[] arrAdded, int nPosition)
        {
            Split(nPosition, arrAdded.Count());
            m_memoryStreamAdd.Write(arrAdded, 0, arrAdded.Length);
        }

        public void Split(int nNewAddStartPosition, int nNewAddLength)
        {
            int nPieceStart = 0;
            for(int i = 0; i < m_listPieces.Count; i++)
            {
                // 만약 Piece 사이 있으면, Split
                if(nNewAddStartPosition >= nPieceStart && nNewAddStartPosition < nPieceStart + m_listPieces[i].nLength)
                {
                    long dwPos = m_listPieces[i].dwStart;
                    int nLength = nNewAddStartPosition - nPieceStart;
                    int nOriginLength = m_listPieces[i].nLength;
                    m_listPieces[i].nLength = nLength;
                    m_listPieces.Insert(i + 1, new cPiece(false, m_memoryStreamAdd.Length, nNewAddLength));
                    m_listPieces.Insert(i + 2, new cPiece(m_listPieces[i].bOriginal, nLength, nOriginLength - nLength));

                    return;
                }
                nPieceStart += m_listPieces[i].nLength;
            }
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
            
            /// - 이 위로부턴 기존 코드 - ///

            byte[] buffer = new byte[1024];
            long dwOffset = start;

            long diffStart = start;

            foreach (cPiece piece in m_listPieces)
            {
                // 만약 벗어나면 넣지 않음
                if(piece.bOriginal && (piece.dwStart + piece.nLength < start || piece.dwStart > end))
                {
                    continue;
                }
                else if(!piece.bOriginal && diffStart + piece.nLength < start || diffStart > end)
                {
                    continue;
                }
                else if(piece.dwStart > end)
                {
                    break;
                }
                int nPieceLenth = piece.nLength;
                if (piece.bOriginal)
                {
                    diffStart = piece.dwStart + piece.nLength;
                    nPieceLenth = (int)(piece.dwStart + (long)piece.nLength - start);
                }
                
                int remain = Math.Min(nPieceLenth, length);

                long dwStartOffset = Math.Max(piece.dwStart, start);

                if (piece.bOriginal)
                {
                    while(remain > 0)
                    {
                        int toRead = Math.Min(1024, remain);
                        m_memoryMappedViewAccessor.ReadArray(dwStartOffset, buffer, (int)(dwOffset - start), toRead);
                        
                        remain -= toRead;

                        dwOffset += toRead;
                    }
                }
                else
                {
                    m_memoryStreamAdd.Position = piece.dwStart;
                    while (remain > 0)
                    {
                        int toRead = Math.Min(1024, remain);
                        m_memoryStreamAdd.Read(buffer, (int)(dwOffset - start), toRead);
                        
                        remain -= toRead;

                        dwOffset += toRead;
                    }
                }
            }
            return Encoding.UTF8.GetString(buffer);
        }

        public int GetLineStartByteOffset(int lineIndex)
        {
            return (int)m_listLineOffsets[lineIndex];
        }

        public int GetIndex(int lineIndex, int column)
        {
            // int lineOffset = (int)m_listLineOffsets[line];
            // return lineOffset + 4 * column;
            string line = GetLine(lineIndex);

            if (column <= 0) return GetLineStartByteOffset(lineIndex);
            if (column >= line.Length)
                return GetLineStartByteOffset(lineIndex) +
                       Encoding.UTF8.GetByteCount(line);

            string sub = line.Substring(0, column);
            return GetLineStartByteOffset(lineIndex) +
                   Encoding.UTF8.GetByteCount(sub);
        }

        public void WriteLine(int lineIndex, int nOffset, byte[] arrNewBuffer)
        {
            long start = m_listLineOffsets[lineIndex];
            m_memoryMappedViewAccessor.WriteArray(start, arrNewBuffer, nOffset, arrNewBuffer.Count());
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
