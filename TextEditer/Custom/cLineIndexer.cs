using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditer
{
    public static class cLineIndexer
    {
        // 한 번에 읽을 버퍼 크기 (너무 크면 캐시 미스, 너무 작으면 syscall 증가)
        private const int BufferSize = 64 * 1024; // 64KB

        public static IEnumerable<LineSpan> Scan(cTextBuffer buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            long fileLength = buffer.m_dwLength;

            byte[] buf = new byte[BufferSize];

            long lineStartOffset = 0;
            long currentOffset = 0;

            while (currentOffset < fileLength)
            {
                int toRead = (int)Math.Min(BufferSize, fileLength - currentOffset);
                int read = buffer.ReadBytes(currentOffset, buf, 0, toRead);
                if (read <= 0)
                    break;

                int i = 0;
                while (i < read)
                {
                    byte b = buf[i];

                    // LF
                    if (b == (byte)'\n')
                    {
                        long lineEndOffset = currentOffset + i;
                        int lineLength = (int)(lineEndOffset - lineStartOffset);

                        yield return new LineSpan(lineStartOffset, lineLength);

                        lineStartOffset = lineEndOffset + 1;
                        i++;
                    }
                    // CRLF
                    else if (b == (byte)'\r')
                    {
                        // 다음 바이트가 LF인지 확인
                        if (i + 1 < read)
                        {
                            if (buf[i + 1] == (byte)'\n')
                            {
                                long lineEndOffset = currentOffset + i;
                                int lineLength = (int)(lineEndOffset - lineStartOffset);

                                yield return new LineSpan(lineStartOffset, lineLength);

                                lineStartOffset = lineEndOffset + 2;
                                i += 2;
                            }
                            else
                            {
                                // 단독 CR (비정상/희귀 케이스)
                                long lineEndOffset = currentOffset + i;
                                int lineLength = (int)(lineEndOffset - lineStartOffset);

                                yield return new LineSpan(lineStartOffset, lineLength);

                                lineStartOffset = lineEndOffset + 1;
                                i++;
                            }
                        }
                        else
                        {
                            // CR이 버퍼 끝에 걸린 경우
                            // 다음 read에서 LF인지 확인해야 하므로 일단 종료
                            break;
                        }
                    }
                    else
                    {
                        i++;
                    }
                }

                currentOffset += read;
            }

            // 마지막 줄 (파일 끝에 개행이 없는 경우)
            if (lineStartOffset < fileLength)
            {
                int length = (int)(fileLength - lineStartOffset);
                yield return new LineSpan(lineStartOffset, length);
            }
            // 파일이 비어 있거나 마지막이 개행으로 끝난 경우
            else if (fileLength > 0 && lineStartOffset == fileLength)
            {
                yield return new LineSpan(fileLength, 0);
            }
        }
    }
}
