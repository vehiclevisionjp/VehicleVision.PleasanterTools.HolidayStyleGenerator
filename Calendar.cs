using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VehicleVision.PleasanetrTools.HolidayStyleGenerator
{
    public class Calendar
    {
        [Name("国民の祝日・休日月日")]
        public DateTime Date { get; set; }

        [Name("国民の祝日・休日名称")]
        public string Title { get; set; }
    }
}
