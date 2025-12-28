using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TextEditer
{
    public struct SearchHit
    {
        public int Line; public int Column; public int Length;
    }
    public static class cSearcher
    {
        public static IEnumerable<SearchHit> Search(
        cDocument doc,
        string keyword,
        CancellationToken token)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));
            if (string.IsNullOrEmpty(keyword))
                yield break;

            for (int line = 0; line < doc.LineCount; line++)
            {
                token.ThrowIfCancellationRequested();

                string text = doc.GetLineText(line);
                if (string.IsNullOrEmpty(text))
                    continue;

                int idx = text.IndexOf(keyword, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    yield return new SearchHit
                    {
                        Line = line,
                        Column = idx,
                        Length = keyword.Length
                    };
                }
            }
        }
    }
}
