using System;
using System.IO;
using System.Net.Mail;
using System.Configuration;

namespace WorkdayDownloader 
{  
	public class Logger : IDisposable
	{
		private static int _ALL = 100;
		private static int _INFO = 75;
		private static int _WARNING = 50;
        private static int _ERROR = 25;
		private static int _OFF = 0;
		private static string m_logDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase ).ToString();
		private static string m_logFile;
		private static DateTime dt = DateTime.Now;

		public int ALL 
		{
			get { return _ALL; } 
		}
		public int ERROR 
		{
			get { return _ERROR; } 
		}
		public int WARNING
		{
			get { return _WARNING; } 
		}
		public int INFO 
		{
			get { return _INFO; } 
		}
		public int OFF 
		{
			get { return _OFF; } 
		} 
		public string logFile
		{
			get { return m_logFile; }
		}
		public Logger()
		{
			if (m_logDirectory.Length >=6)
			{
				if (m_logDirectory.Substring(0,6) == "file:\\")
				{
					m_logDirectory = m_logDirectory.Substring(6,m_logDirectory.Length - 6) + "\\logs\\";
					m_logFile = m_logDirectory + dt.ToString("yyyyMMdd") + ".log";
					if (!Directory.Exists(m_logDirectory))
					{
						Directory.CreateDirectory(m_logDirectory);
					}
				}
			}
		}
		public void emailLog()
		{
			MailMessage objMailMessage = new MailMessage();
			objMailMessage.Body = "Please review the attached log file.";
			objMailMessage.From = new MailAddress(ConfigurationManager.AppSettings["EmailFrom"]);
            objMailMessage.To.Add(ConfigurationManager.AppSettings["EmailTo"]);
			objMailMessage.Subject = "-- WorkdayDownloader Log --";
			if (File.Exists(m_logFile)) 
			{
				Attachment objAttachment = new Attachment(m_logFile.ToString());
				objMailMessage.Attachments.Add(objAttachment);
			}
            SmtpClient SmtpMail = new SmtpClient(ConfigurationManager.AppSettings["SMTPServer"]);
			SmtpMail.Send( objMailMessage );
		}
		public void backup()
		{

			int intCtr = 1;
			string filePath1 = m_logFile;
			string filePath2 = m_logDirectory + dt.ToString("yyyyMMdd") + intCtr.ToString("000") + ".log";
			while(File.Exists(filePath2) && intCtr < 999) 
			{
				intCtr++;
				filePath2 = m_logDirectory + dt.ToString("yyyyMMdd") + intCtr.ToString("000") + ".log";
			}
			if (File.Exists(filePath1))
			{
				File.Move(filePath1,filePath2);
			}
		}
		public void append(String message, int level) 
		{
			int logLevel = _OFF;
			String strLogLevel = ConfigurationManager.AppSettings["logLevel"].ToString();
			switch(strLogLevel) 
			{
				case "ALL":
					logLevel = _ALL;
					break;
				case "INFO": 
					logLevel = _INFO;
					break;
				case "WARNING":
					logLevel = _WARNING;
					break;
                case "ERROR":
                    logLevel = _ERROR;
                    break;
				default:
					logLevel = _OFF;
					break;
			}
			if (logLevel >= level) 
			{
				if (!File.Exists(m_logFile)) 
				{
					FileStream fs = File.Create(m_logFile);
					fs.Close();
				}
				try 
				{
					DateTime dtLog = DateTime.Now;
					StreamWriter sw = File.AppendText(m_logFile);
					sw.WriteLine(dtLog.ToString("hh:mm:ss") + "\t" + message);
					sw.Flush();
					sw.Close();
				} 
				catch (Exception e) 
				{
					Console.WriteLine(e.Message.ToString());
				}
			}
		}
        public void Dispose()
        { }
	}
}
