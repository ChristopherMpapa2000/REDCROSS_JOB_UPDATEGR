using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Job_UpdateGR.Item
{
    class ItemConfig
    {
        public static string dbConnectionString
        {
            get
            {
                var ServarName = ConfigurationManager.AppSettings["ServarName"];
                var Database = ConfigurationManager.AppSettings["Database"];
                var Username_database = ConfigurationManager.AppSettings["Username_database"];
                var Password_database = ConfigurationManager.AppSettings["Password_database"];
                var dbConnectionString = $"data source={ServarName};initial catalog={Database};persist security info=True;user id={Username_database};password={Password_database};Connection Timeout=200";

                if (!string.IsNullOrEmpty(dbConnectionString))
                {
                    return dbConnectionString;
                }
                return string.Empty;
            }
        }
        public static string LogFile
        {
            get
            {
                var LogFile = System.Configuration.ConfigurationSettings.AppSettings["LogFile"];
                if (!string.IsNullOrEmpty(LogFile))
                {
                    return (LogFile);
                }
                return string.Empty;
            }
        }
        public static string Docno
        {
            get
            {
                var tempCode = ConfigurationManager.AppSettings["Docno"];

                if (!string.IsNullOrEmpty(tempCode))
                {
                    return tempCode;
                }
                return string.Empty;
            }
        }
        public static string TemplateId
        {
            get
            {
                var TemplateId = ConfigurationManager.AppSettings["TemplateId"];

                if (!string.IsNullOrEmpty(TemplateId))
                {
                    return TemplateId;
                }
                return string.Empty;
            }
        }
        public static int IntervalTimeDay
        {
            //ตั้งค่าเวลา
            get
            {
                var IntervalTimeDay = ConfigurationManager.AppSettings["IntervalTimeDay"];
                if (!string.IsNullOrEmpty(IntervalTimeDay))
                {
                    return Convert.ToInt32(IntervalTimeDay);
                }
                return -10;
            }
        }
    }
}
