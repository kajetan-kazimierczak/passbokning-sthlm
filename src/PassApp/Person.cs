using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassApp
{
    internal class Person
    {
        public string Forman { get; set; }
        public string Efternamn { get; set; }
    }

    internal class Order
    {
        public string ContactPhone { get; set; }
        public string ContactEmail { get; set; }
        public List<Person> People { get; set; } = new List<Person>();
        public DateOnly NotLaterThan { get; set; }

        public List<PassportOffice> PassportOffices = new List<PassportOffice>();
    }

    internal enum PassportOffice
    {
        Flemingsberg = 38,
        Globen = 40,
        Haninge = 46,
        Rinkeby = 113,
        Nacka = 45,
        Norrtalje = 44,
        Sollentuna = 43,
        Solna = 42,
        SthlmCity = 41,
        Sodertalje = 47,
        SodraRoslagenTaby = 48
    }

    internal class TimeBooking
    {
        public string FormId => "2";
        public string ReservedServiceTypeId { get; set; } //2164
        public string ReservedSectionId { get; set; } //	113
        public string NQServiceTypeId { get; set; } = "1"; //	1
        public string SectionId { get; set; } = "0"; //	0
        public string FromDateString { get; set; } = "2022-09-19";
        public string NumberOfPeople { get; set; } //	3
        public string SearchTimeHour { get; set; } = "10"; //	11
        public string RegionId => "0";
        public string ReservedDateTime { get; set; } //	2022-09-19 12:30:00
        public string Next => "Nästa";
    }
}
