using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditer.Custom
{
    public interface IRawFileReader : IDisposable
    {
        long Length { get; }

        // 최소 원시 기능
        int ReadBytes(long fileOffset, byte[] dest, int destIndex, int count);

        // 옵션: 문자열 디코딩(자주 쓰면)
        string ReadUtf8(long fileOffset, int byteCount);

        // UTF-8 문자 경계 유틸
        int GetNextUtf8CharByteLength(long fileOffset);      // offset 위치의 첫 바이트 기준
        int GetPrevUtf8CharByteLength(long fileOffsetMinus1); // 바로 이전 문자로 이동할 때 유용
    }
}
