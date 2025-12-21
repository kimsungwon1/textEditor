using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditer
{
    public class cLineDeltaFenwick
    {
        long[] tree;

        public cLineDeltaFenwick(int n)
        {
            tree = new long[n + 1];
        }

        public void AddDelta(int idx, long delta)
        {
            for (int i = idx + 1; i < tree.Length; i += i & -i)
                tree[i] += delta;
        }

        public long Query(int idx)
        {
            long sum = 0;
            for (int i = idx + 1; i > 0; i -= i & -i)
                sum += tree[i];
            return sum;
        }
        public long QueryExclusive(int idx)
        {
            if (idx <= 0) return 0;
            return Query(idx - 1);
        }
    }
}
