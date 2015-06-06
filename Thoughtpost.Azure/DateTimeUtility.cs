using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thoughtpost.Azure
{
    public partial class DateTimeUtility
    {
        public static string InverseTimeKey(DateTime dt)
        {
            // FROM ALEXANDRE BRISEBOIS
            var inverseTimeKey = DateTime.MaxValue.Subtract(dt).Ticks;

            inverseTimeKey++;

            return inverseTimeKey.ToString("d19");
        }

        public static string MaxTimeKey
        {
            get
            {
                return InverseTimeKey(DateTime.MaxValue);
            }
        }

        public static string MinTimeKey
        {
            get
            {
                return InverseTimeKey(DateTime.MinValue);
            }
        }

        public static DateTime GetCurrentTimeByTimeZone(string timeZone)
        {
            TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            DateTime dtLocal = DateTime.UtcNow.AddHours(tst.GetUtcOffset(DateTime.Now).Hours);

            return dtLocal;
        }

        public static DateTime GetCurrentTimeByTimeZone(string timeZone, DateTime when)
        {
            TimeZoneInfo tst = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
            DateTime dtLocal = DateTime.UtcNow.AddHours(tst.GetUtcOffset(when).Hours);

            return dtLocal;
        }
        
        public static DateTime GetESTNow()
        {
            return GetCurrentTimeByTimeZone("Eastern Standard Time");
        }
    }
}

