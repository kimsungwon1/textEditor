using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TextEditer
{
    public sealed class cFsWatchersManager
    {
        private static readonly Lazy<cFsWatchersManager> _instance = new Lazy<cFsWatchersManager>(() => new cFsWatchersManager());

        private cFsWatchersManager()
        {
        }

        public static cFsWatchersManager Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public void AddPath(string sFileFullPath)
        {
            string sDirectoryName = Path.GetDirectoryName(sFileFullPath);
            string sFileName = Path.GetFileName(sFileFullPath);
        }
    }
}
