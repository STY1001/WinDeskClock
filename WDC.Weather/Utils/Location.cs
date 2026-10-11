using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDC.Weather.Utils
{
    public class Location
    {
        public string? Name { get; set; }
        public string? Country { get; set; }
        public string? Admin1 { get; set; }
        public string? Admin2 { get; set; }
        public string? Admin3 { get; set; }
        public string? Admin4 { get; set; }
        public double? Elevation { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string ToStringAutosuggest()
        {
            return $"{Name}, {Admin4}{(Admin4 == null ? "" : ", ")}{Admin3}{(Admin3 == null ? "" : ", ")}{Admin2}{(Admin2 == null ? "" : ", ")}{Admin1}{(Admin1 == null ? "" : ", ")}{Country} ({Latitude}°N, {Longitude}°E)";
        }
    }
}
