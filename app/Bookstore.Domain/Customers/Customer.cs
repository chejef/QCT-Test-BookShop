using System;

namespace Bookstore.Domain.Customers
{
    public class Customer : Entity
    {
        public string Sub { get; set; } = null!;

        public string Username { get; set; } = null!;

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string FullName => $"{FirstName} {LastName}";

        public string Email { get; set; } = null!;

        public DateTime? DateOfBirth { get; set; }

        public string? Phone { get; set; }
    }
}