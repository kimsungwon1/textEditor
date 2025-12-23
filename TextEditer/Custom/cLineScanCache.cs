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
        }
        public bool bChangeReflected = true;
        public List<long> listScannedPos;
        public int ScannedLineStart;
        public int ScannedLineCount;
        public void SetLineAndCount(int nLine, int count)
        {
            if (ScannedLineStart != nLine || ScannedLineCount != count)
            {
                bChangeReflected = false;
                ScannedLineStart = nLine;
                ScannedLineCount = count;
            }
        }
        public void PeekLineAndPos(int targetLine, out long dwPos, out int nLine)
        {
            if (ScannedLineStart <= targetLine && targetLine <= ScannedLineStart + ScannedLineCount)
            {
                if (ScannedLineStart < listScannedPos.Count)
                {
                    nLine = ScannedLineStart;
                    dwPos = listScannedPos[ScannedLineStart];
                }
                else if (ScannedLineStart >= listScannedPos.Count)
                {
                    nLine = listScannedPos.Count - 1;
                    dwPos = listScannedPos.Last();
                }
                else
                {
                    nLine = 0;
                    dwPos = 0;
                }
            }
            else
            {
                int lineStart = Math.Min(listScannedPos.Count - 2, ScannedLineStart);
                dwPos = listScannedPos[lineStart];
                nLine = (int)(listScannedPos[lineStart + 1] - listScannedPos[lineStart]);
            }
        }
    }
}
