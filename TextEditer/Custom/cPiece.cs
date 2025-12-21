using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditer
{
    public enum PieceSource { Original, Add }
    public class cPiece
    {
        public PieceSource Source; // false 면 Add
        public long Start;
        public int Length;

        public cPiece(PieceSource eSource, long dwStart, int nLen)
        {
            Source = eSource;
            this.Start = dwStart;
            Length = nLen;
        }
    }
}
