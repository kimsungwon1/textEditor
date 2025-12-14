using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditer
{
    public interface iPresenter
    {
        void LoadData(string sFilePath);
        void SaveData(string sFilePath);
    }
}
