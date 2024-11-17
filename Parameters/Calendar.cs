using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VehicleVision.PleasanetrTools.HolidayStyleGenerator.Parameters
{
    public class Calendar
    {
        public string CalendarUrl { get; set; } = "https://www8.cao.go.jp/chosei/shukujitsu/syukujitsu.csv";

        public int FirstDayOfWeek { get; set; } = 1;

        public string SaturdayColor { get; set; } = "#add8e6";
        public string SundayColor { get; set; } = "#ffc0cb";
        public string HolidayColor { get; set; } = "#ffc0cb";

    }
}
