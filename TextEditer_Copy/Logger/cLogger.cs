using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

namespace TextEditer
{
    public enum eLogType
    {
        SYSTEM,
        WARNING,
        ERROR,
        INFO,
        USER_ACTION
    }
    public class cLog
    {
        public cLog(eLogType eType, string sMessage)
        {
            m_eType = eType; m_sMessage = sMessage;
        }
        public eLogType m_eType
        {
            get
            {
                return m_eType;
            }
            set
            {
                m_eType = value;
            }
        }
        public string m_sMessage
        {
            get
            {
                return m_sMessage;
            }
            set
            {
                m_sMessage = value;
            }
        }
    }
    public sealed class cLogger
    {
        private static readonly Lazy<cLogger> _instance = new Lazy<cLogger>(() => new cLogger());
        private const string m_sLogPath = "../../app.log";

        private cLogger()
        {
        }

        public static cLogger Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        public void AddLog(eLogType eType, Exception exception)
        {
            string sNewMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {eType.ToString()} | {exception.Message} | StackTrace :{exception.StackTrace}";
            try
            {
                using (StreamWriter sw = new StreamWriter(m_sLogPath, true))
                {
                    sw.WriteLine(sNewMessage);
                }
            }
            catch (IOException e)
            {
                MessageBox.Show(e.Message + $"\n StackTrace :{e.StackTrace}");
            }

        }

        public void AddLog(eLogType eType, string message)
        {
            string sNewMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {eType.ToString()} | {message}";
            try
            {
                using (StreamWriter sw = new StreamWriter(m_sLogPath, true))
                {
                    sw.WriteLine(sNewMessage);
                }
            }
            catch (IOException e)
            {
                MessageBox.Show(e.Message + $"\n StackTrace :{e.StackTrace}");
            }
        }
    }
}
