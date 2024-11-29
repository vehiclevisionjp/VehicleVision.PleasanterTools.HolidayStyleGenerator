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
        public int SaturdayIndex { get; set; } = 6;
        public int SundayIndex { get; set; } = 7;
        public string SaturdayBackgroundColor { get; set; } = "#add8e6";
        public string SundayBackgroundColor { get; set; } = "#ffc0cb";
        public string HolidayBackgroundColor { get; set; } = "#ffc0cb";
    }
}
