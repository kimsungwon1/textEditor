using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditer
{
    public class cLineBlock
    {
        public long dwDocStart;
        public int nByteCount;
        public List<int> listLineStartOffsets = new List<int>();

        public int nLineCount => listLineStartOffsets.Count;
        public long dwDocEnd => dwDocStart + nByteCount;
    }

    public class cLineIndexBlocks
    {
        public const int TARGET_BLOCK_BYTES = 128 * 1024;
        public const int MAX_BLOCK_BYTES = 256 * 1024;
        public const int MIN_BLOCK_BYTES = 32 * 1024;
        public const int EXTRA_CAPACITY = 16;

        List<cLineBlock> m_listBlocks = new List<cLineBlock>();
        cLineDeltaFenwick m_lineDelta;

        public cLineIndexBlocks(int initialBlockCount)
        {
            m_lineDelta = new cLineDeltaFenwick(initialBlockCount + EXTRA_CAPACITY);
        }

        public void BuildFromDocumemt(long dwTotalLength, ITextBuffer textBuffer)
        {
            m_listBlocks.Clear();

            long dwPos = 0;

            while(dwPos < dwTotalLength)
            {
                long dwSize = Math.Min(TARGET_BLOCK_BYTES, dwTotalLength - dwPos);

                cLineBlock newBlock = new cLineBlock
                {
                    dwDocStart = dwPos,
                    nByteCount = (int)dwSize
                };

                BuildBlock(newBlock, textBuffer);
                m_listBlocks.Add(newBlock);
                dwPos += dwSize;
            }

            int nLineCount = GetLineCount();
            m_lineDelta = new cLineDeltaFenwick(nLineCount + EXTRA_CAPACITY);
        }

        public void BuildBlock(cLineBlock block, ITextBuffer textBuffer)
        {
            block.listLineStartOffsets.Clear();

            block.listLineStartOffsets.Add(0);

            const int BUF = 8192;
            byte[] bytes = new byte[BUF];

            long dwPos = block.dwDocStart;
            long dwEnd = block.dwDocEnd;
            int rel = 0;

            while(dwPos < dwEnd)
            {
                long dwToRead = Math.Min(dwEnd - dwPos, BUF);

                // bytes = textBuffer.ReadRangeBytes(dwPos, (int)dwToRead);
                long byteRead = Math.Min(dwPos + dwToRead, bytes.Length);
                for (long dwIndex = dwPos; dwIndex < byteRead; dwIndex++, rel++)
                {
                    if(bytes[dwIndex - dwPos] == '\n')
                    {
                        block.listLineStartOffsets.Add(rel + 1);
                    }
                }

                dwPos += dwToRead;
            }
        }
        public long GetLineStartOffset(int nLine)
        {
            int nCurrentLine = 0;
            for (int i = 0; i < m_listBlocks.Count; i++)
            {
                cLineBlock block = m_listBlocks[i];
                if(nCurrentLine <= nLine && nLine < nCurrentLine + block.nLineCount)
                {
                    int nInnerLine = nLine - nCurrentLine;
                    long dwDelta = m_lineDelta.QueryExclusive(nLine);
                    return block.dwDocStart+ block.listLineStartOffsets[nInnerLine] + dwDelta;
                }
                
                nCurrentLine += block.nLineCount;
            }
            cLineBlock lastBlock = m_listBlocks.Last();
            return lastBlock.dwDocEnd;
        }

        public int GetLineByBlockIndex(int blockIndex)
        {
            int nLineCount = 0;
            for(int i = 0; i < blockIndex; i++)
            {
                nLineCount += m_listBlocks[i].nLineCount;
            }
            return nLineCount;
        }

        public int GetLineByByteOffset(long dwByteOffset)
        {
            int nCurrentLine = 0;

            for(int i = 0; i < m_listBlocks.Count; i++)
            {
                cLineBlock block = m_listBlocks[i];

                long dwStart = block.dwDocStart + m_lineDelta.QueryExclusive(GetLineByBlockIndex(i));
                long dwEnd = dwStart + block.nByteCount;

                if(dwByteOffset < dwEnd)
                {
                    int nRel = (int)(dwByteOffset - dwStart);
                    for(int nLine = 0; nLine < block.listLineStartOffsets.Count; nLine++)
                    {
                        int nLineStartOffset = block.listLineStartOffsets[nLine];
                        if(nLineStartOffset > nRel)
                        {
                            return nLineStartOffset + nCurrentLine;
                        }
                    }
                }
                nCurrentLine += block.nByteCount;
            }
            cLineBlock lastBlock = m_listBlocks.Last();
            return lastBlock.nLineCount - 1;
        }

        public int GetBlockIndexByBytes(long dwByteOffset)
        {
            for (int i = 0; i < m_listBlocks.Count; i++)
            {
                cLineBlock block = m_listBlocks[i];

                long dwStart = block.dwDocStart + m_lineDelta.QueryExclusive(GetLineByBlockIndex(i));
                long dwEnd = dwStart + block.nByteCount;

                if (dwStart <= dwByteOffset && dwByteOffset < dwEnd)
                {
                    return i;
                }
            }
            return m_listBlocks.Count - 1;
        }

        public long GetSplitPos(long startPos, long dwMid, long endPos, ITextBuffer textBuffer)
        {
            // int splitCount1 = (int)(dwMid - startPos);
            // int splitCount2 = (int)(endPos - dwMid);
            // 
            // byte[] secondSplit = textBuffer.ReadRangeBytes(dwMid, splitCount2);
            // for(long i = 0; i < splitCount2; i++)
            // {
            //     if(secondSplit[i] == '\n')
            //     {
            //         return dwMid + i;
            //     }
            // }
            // byte[] firstSplit = textBuffer.ReadRangeBytes(startPos, splitCount1);
            // for(long i = splitCount1 - 1; i >= 0; i--)
            // {
            //     if(firstSplit[i] == '\n')
            //     {
            //         return startPos + i;
            //     }
            // }
            return dwMid;
        }

        public void SplitBlock(int blockIndex, ITextBuffer textBuffer)
        {
            cLineBlock block = m_listBlocks[blockIndex];
            int nMid = (int)block.dwDocStart + block.nByteCount / 2;

            long splitPos = GetSplitPos(block.dwDocStart, nMid, block.dwDocStart + block.nByteCount, textBuffer);

            cLineBlock firstBlock = new cLineBlock { dwDocStart = block.dwDocStart, nByteCount = block.nByteCount / 2 };
            cLineBlock secondBlock = new cLineBlock { dwDocStart = nMid, nByteCount = block.nByteCount / 2 };

            BuildBlock(firstBlock, textBuffer);
            BuildBlock(secondBlock, textBuffer);

            m_listBlocks[blockIndex] = firstBlock;
            m_listBlocks.Insert(blockIndex + 1, secondBlock);
        }

        public void MergeBlock(int blockIndex, ITextBuffer textBuffer)
        {
            if(blockIndex >= m_listBlocks.Count - 1 || blockIndex < 0)
            {
                return;
            }
            cLineBlock blockFirst = m_listBlocks[blockIndex];
            cLineBlock blockSecond = m_listBlocks[blockIndex + 1];

            long newBlockBytesCount = blockFirst.nByteCount + blockSecond.nByteCount;

            cLineBlock newBlock = new cLineBlock { dwDocStart = blockFirst.dwDocStart, nByteCount = (int)newBlockBytesCount, listLineStartOffsets = new List<int>() };

            BuildBlock(newBlock, textBuffer);

            m_listBlocks[blockIndex] = newBlock;

            m_listBlocks.RemoveAt(blockIndex + 1);
        }

        public void OnInsert(long byteOffset, byte[] bytesInserted, ITextBuffer textBuffer)
        {
            int blockIndex = GetBlockIndexByBytes(byteOffset);

            m_listBlocks[blockIndex].nByteCount += bytesInserted.Length;

            // ctrl+v처럼 여러 줄바꿈 있는 경우도 고려할 것
            if (bytesInserted.Contains((byte)('\n')))
            {
                BuildBlock(m_listBlocks[blockIndex], textBuffer);

                if (m_listBlocks[blockIndex].nByteCount > MAX_BLOCK_BYTES)
                {
                    SplitBlock(blockIndex, textBuffer);
                }
            }
            else
            {
                m_lineDelta.AddDelta(GetLineByByteOffset(byteOffset), bytesInserted.Length);
            }
        }
        public void OnDelete(long byteOffset, byte[] bytesToDelete, ITextBuffer textBuffer)
        {
            int blockIndex = GetBlockIndexByBytes(byteOffset);

            m_listBlocks[blockIndex].nByteCount -= bytesToDelete.Length;

            // ctrl+v처럼 여러 줄바꿈 있는 경우도 고려할 것
            if(bytesToDelete.Contains((byte)('\n')))
            {
                BuildBlock(m_listBlocks[blockIndex], textBuffer);

                if (m_listBlocks[blockIndex].nByteCount < MIN_BLOCK_BYTES)
                {
                    MergeBlock(blockIndex, textBuffer);
                }
            }
            else
            {
                m_lineDelta.AddDelta(GetLineByByteOffset(byteOffset), -bytesToDelete.Length);
            }
        }

        public int GetLineCount()
        {
            int nLineCount = 0;

            foreach(cLineBlock block in m_listBlocks)
            {
                nLineCount += block.nLineCount;
            }

            return nLineCount;
        }
    }

}
