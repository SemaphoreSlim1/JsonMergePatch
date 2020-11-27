namespace Example.Model
{
    public class Person
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        public Address Address { get; set; }
    }

    public class Address
    {
        public string StreetAddressLine1 { get; set; }
        public string StreetAddressLine2 { get; set; }
        public string City { get; set; }
        public State State { get; set; }
        public string Zip { get; set; }
    }

    public class State
    {
        public string Abbreviation { get; set; }
        public string Name { get; set; }
    }
}
