using System.Collections.Generic;

namespace Bookstore.Web.ViewModel.Address
{
    public class AddressIndexViewModel
    {
        public List<AddressIndexItemViewModel> Items { get; set; } = new List<AddressIndexItemViewModel>();

        public AddressIndexViewModel(IEnumerable<Domain.Addresses.Address> addresses)
        {
            foreach (var address in addresses)
            {
                Items.Add(new AddressIndexItemViewModel
                {
                    Id = address.Id,
                    AddressLine1 = address.AddressLine1,
                    AddressLine2 = address.AddressLine2,
                    City = address.City,
                    State = address.State,
                    Country = address.Country,
                    ZipCode = address.ZipCode
                });
            }
        }
    }

    public class AddressIndexItemViewModel
    {
        public int Id { get; set; }

        public string AddressLine1 { get; set; } = string.Empty;

        public string AddressLine2 { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string ZipCode { get; set; } = string.Empty;
    }
}
