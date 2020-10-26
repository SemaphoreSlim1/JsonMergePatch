using System;
using System.Collections.Generic;

namespace Mergable
{
    public class Person
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public Address Address { get; set; }

        public IList<Car> Cars { get; set; }
        public IList<string> NickNames { get; set; }
    }

    public class Address
    {
        public int HouseNumber { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
    }

    public class Car
    {
        public int Year { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
    }
}
