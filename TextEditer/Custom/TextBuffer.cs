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
        private readonly List<StringBuilder> _lines = new List<StringBuilder>();

        public int LineCount => _lineOffsets.Count;

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;

        private readonly List<long> _lineOffsets = new List<long>();

        private long _fileLength;

        Dictionary<int, string> _lineCache = new Dictionary<int, string>();

        public ITextBuffer()
        {
            // 항상 최소 1줄
            _lines.Add(new StringBuilder());
        }

        public string GetLine(int index)
        {
            if (index < 0 || index >= _lineOffsets.Count)
                return string.Empty;

            long start = _lineOffsets[index];
            long end = (index + 1 < _lineOffsets.Count)
                ? _lineOffsets[index + 1] - 1
                : _fileLength;

            int length = (int)(end - start);
            if (length <= 0)
                return string.Empty;

            byte[] buffer = new byte[length];
            _accessor.ReadArray(start, buffer, 0, length);

            // 개행 문자 제거
            if (buffer.Length > 0 && buffer[buffer.Length - 1] == '\r') // buffer[^1]
                length--;

            return Encoding.UTF8.GetString(buffer, 0, length);
        }

        public int GetLineLength(int index)
        {
            if (index < 0 || index >= _lines.Count)
                return 0;

            return _lines[index].Length;
        }

        private void EnsureLineExists(int line)
        {
            while (_lines.Count <= line)
            {
                _lines.Add(new StringBuilder());
            }
        }

        public void InsertChar(ref TextCursor cursor, char c)
        {
            EnsureLineExists(cursor.Line);

            StringBuilder line = _lines[cursor.Line];

            if (cursor.Column > line.Length)
                cursor.Column = line.Length;

            line.Insert(cursor.Column, c);
            cursor.Column++;
        }

        public void InsertNewLine(ref TextCursor cursor)
        {
            EnsureLineExists(cursor.Line);

            var line = _lines[cursor.Line];

            string right =
                cursor.Column < line.Length
                    ? line.ToString(cursor.Column, line.Length - cursor.Column)
                    : string.Empty;

            if (cursor.Column < line.Length)
                line.Length = cursor.Column;

            var newLine = new StringBuilder(right);
            _lines.Insert(cursor.Line + 1, newLine);

            cursor.Line++;
            cursor.Column = 0;
        }

        public void Backspace(ref TextCursor cursor)
        {
            if (cursor.Line < 0 || cursor.Line >= _lines.Count)
                return;

            if (cursor.Column > 0)
            {
                _lines[cursor.Line].Remove(cursor.Column - 1, 1);
                cursor.Column--;
            }
            else if (cursor.Line > 0)
            {
                int prevLen = _lines[cursor.Line - 1].Length;
                _lines[cursor.Line - 1].Append(_lines[cursor.Line]);
                _lines.RemoveAt(cursor.Line);

                cursor.Line--;
                cursor.Column = prevLen;
            }
        }

        public void LoadFromFile(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            _fileLength = fileInfo.Length;

            _mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);

            _accessor = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            
            // _lines.Clear();
            // 
            // StreamReader reader = new StreamReader(path, Encoding.UTF8);
            // 
            // while (!reader.EndOfStream)
            // {
            //     _lines.Add(new StringBuilder(reader.ReadLine()));
            // }
            // 
            // if (_lines.Count == 0)
            //     _lines.Add(new StringBuilder());
        }

        private void BuildLineIndex()
        {
            _lineOffsets.Clear();
            _lineOffsets.Add(0); // 첫 줄

            for (long i = 0; i < _fileLength; i++)
            {
                byte b = _accessor.ReadByte(i);
                if (b == (byte)'\n')
                {
                    long next = i + 1;
                    if (next < _fileLength)
                        _lineOffsets.Add(next);
                }
            }
        }

        public void Dispose()
        {

        }
    }
}
