using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditer
{
    public interface iTabTextBoxPresenter : iPresenter
    {
        void SaveDataAsNewName();
        void Close(bool bSave);
    }
}
