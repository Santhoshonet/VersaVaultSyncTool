using System;

namespace Testconsole
{
    class Program
    {
        static void Main()
        {
            foreach (var systemTimeZone in TimeZoneInfo.GetSystemTimeZones())
            {
                if (systemTimeZone.Id.ToLower() == "utc")
                {
                    DateTime localtime = DateTime.Now.ToUniversalTime();
                    TimeZoneInfo usTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(systemTimeZone.Id);
                    var dt = TimeZoneInfo.ConvertTimeToUtc(localtime, usTimeZoneInfo);
                    break;
                }
                //Console.WriteLine(systemTimeZone.Id);
            }
            //Console.Read();
            return;
            //DateTime localtime = DateTime.Now;
            //string uscanadazone = "US & Canada";
            //TimeZoneInfo usTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(uscanadazone);
            //var dt = TimeZoneInfo.ConvertTimeToUtc(localtime, usTimeZoneInfo);
            return;
            string dateStr = "Sat, 26 Nov 2011 12:59:00 -06:00";
            DateTime convertedDate = DateTime.SpecifyKind(DateTime.Parse(dateStr), DateTimeKind.Utc);
            convertedDate.ToUniversalTime();
            return;
            var mytime = convertedDate.ToLocalTime();
            Console.WriteLine(mytime.ToString("R"));
        }
    }
}