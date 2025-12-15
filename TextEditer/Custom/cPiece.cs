using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditer
{
    public class cPiece
    {
        public bool bOriginal; // false 면 Add
        public long dwStart;
        public int nLength;

        public cPiece(bool bOrigin, long dwStart, int nLen)
        {
            bOriginal = bOrigin;
            this.dwStart = dwStart;
            nLength = nLen;
        }
    }
}
