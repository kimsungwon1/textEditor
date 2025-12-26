using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditer
{
    class cLineScanCache
    {
        public cLineScanCache(int offsets)
        {
            listScannedPos = new List<long>(offsets);
            listScannedPos.Add(0);
        }
        public bool bChangeReflected = true;
        public List<long> listScannedPos;
        public int ScannedLineStart;
        public int ScannedLineCount;

        public int MaxCachedLine => listScannedPos.Count - 1;

        public void SetLineAndCount(int nLine, int count)
        {
            if (ScannedLineStart != nLine || ScannedLineCount != count)
            {
                ScannedLineStart = nLine;
                ScannedLineCount = count;
            }
        }
        public bool TryGetCachedLine(int targetLine, out long pos)
        {
            if (targetLine >= 0 && targetLine < listScannedPos.Count && listScannedPos[targetLine] >= 0)
            {
                pos = listScannedPos[targetLine];
                return true;
            }

            pos = -1;
            return false;
        }
        public void PeekLineAndPos(int targetLine, out long dwPos, out int nLine)
        {
            dwPos = 0;
            nLine = 0;

            if (listScannedPos.Count == 0)
                return;

            int maxLine = Math.Min(targetLine, listScannedPos.Count - 1);

            for (int i = maxLine; i >= 0; i--)
            {
                if (listScannedPos[i] >= 0)
                {
                    nLine = i;
                    dwPos = listScannedPos[i];
                    return;
                }
            }

            nLine = 0;
            dwPos = listScannedPos[0];
        }
        public void StoreLineStart(int line, long startPos)
        {
            if (line < 0) return;
            if (startPos < 0) return;

            if (line > 0 && line - 1 < listScannedPos.Count)
            {
                long prev = listScannedPos[line - 1];
                if (prev >= 0 && startPos <= prev)
                {
                    return;
                }
            }

            while (listScannedPos.Count <= line)
                listScannedPos.Add(-1);

            if (listScannedPos[line] == -1)
                listScannedPos[line] = startPos;
        }
       
        public void InvalidateFromLine(int line)
        {
            if (line < 0)
                line = 0;

            int nCompare = ScannedLineStart + ScannedLineCount; // listScannedPos.Count;

            if(line < listScannedPos.Count)
            {
                // for(int i = line; i < nCompare - line; i++)
                // {
                //     listScannedPos[i] = -1;
                // }
                listScannedPos.RemoveRange(line, listScannedPos.Count - line);
            }
        }
    }
}
