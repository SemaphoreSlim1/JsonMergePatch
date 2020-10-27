using System;
using System.Collections.Generic;

namespace JsonMergePatch.UnitTests.ExampleModel
{
    public class SimplifiedPerson
    {
        public string FullName { get; set; }
        public string Age { get; set; }
        public string AddressLine1 { get; set; }
        public string Creation { get; set; }
    }

    public class Person
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public Address Address { get; set; }

        public IList<Car> Cars { get; set; }

        //a collection of primitives
        public IList<string> NickNames { get; set; }
        public DateTime? CreationDate { get; set; }
    }

    public class Address
    {
        public int HouseNumber { get; set; }
        public string City { get; set; }
        public State State { get; set; }
        public string Zip { get; set; }
    }

    public class State
    {
        public string FullName { get; set; }
        public string Abbreviation { get; set; }
    }

    public class Car
    {
        public int Year { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
    }
}
