using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditer
{
    abstract public class cPiece
    {
        public bool IsAdd; // true 면 추가 문자, false면 삭제
        public long Start;
        public int LineBreaks;

        abstract public void Merge(cPiece newPiece, long insertPos, out bool bMasterAlive, out bool bMergeeAlive);
    }

    public class cPieceToAdd : cPiece
    {
        public string Content;

        public cPieceToAdd(long start, string content, int lineBreaks = 0)
        {
            IsAdd = true;
            Start = start;
            Content = content;
            LineBreaks = lineBreaks;
        }

        public override void Merge(cPiece newPiece, long insertPos, out bool bMasterAlive, out bool bNewOneAlive)
        {
            if(newPiece.IsAdd)
            {
                cPieceToAdd pieceToAdd = newPiece as cPieceToAdd;

                Content = Content.Insert((int)insertPos, pieceToAdd.Content);
                bMasterAlive = true;
                bNewOneAlive = false;
                LineBreaks += pieceToAdd.LineBreaks;
            }
            else
            {
                cPieceToRemove pieceToRemove = newPiece as cPieceToRemove;
                int startPos; int removeLength;
                if(insertPos < 0)
                {
                    startPos = 0;
                }
                else
                {
                    startPos = (int)insertPos;
                }

                removeLength = Math.Min(Content.Length - startPos, pieceToRemove.CountToRemove);
                string sStrToRemove = Content.Substring(startPos, removeLength);
                int lineBreakDeletedCount = 0;
                for (int i = 0; i < sStrToRemove.Length; i++)
                {
                    if(sStrToRemove[i] == '\n')
                    {
                        lineBreakDeletedCount++;
                    }
                }
                LineBreaks -= lineBreakDeletedCount;

                Content = Content.Remove(startPos, removeLength);
               
                pieceToRemove.CountToRemove -= removeLength;
                if (String.IsNullOrEmpty(Content))
                {
                    bMasterAlive = false;
                }
                else
                {
                    bMasterAlive = true;
                }
                if (pieceToRemove.CountToRemove == 0)
                {
                    bNewOneAlive = false;
                }
                else
                {
                    bNewOneAlive = true;
                }
            }
        }
    }
    public class cPieceToRemove : cPiece
    {
        public int CountToRemove;

        public cPieceToRemove(long start, int count, int lineBreaks = 0)
        {
            IsAdd = false;
            Start = start;
            CountToRemove = count;
            LineBreaks = lineBreaks;
        }

        public override void Merge(cPiece newPiece, long insertPos, out bool bMasterAlive, out bool bNewOneAlive)
        {
            if (!newPiece.IsAdd)
            {
                cPieceToRemove pieceToRemove = newPiece as cPieceToRemove;
                CountToRemove += pieceToRemove.CountToRemove;
                bMasterAlive = true;
                bNewOneAlive = false;
            }
            else
            {
                bMasterAlive = true;
                bNewOneAlive = true;
            }
            LineBreaks += newPiece.LineBreaks;
        }
    }
}
