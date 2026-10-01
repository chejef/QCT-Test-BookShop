using Bookstore.Domain;
using Bookstore.Domain.Offers;

namespace Bookstore.Web.ViewModel.Resale
{
    public class ResaleIndexViewModel
    {
        public List<ResaleIndexItemViewModel> Items { get; set; } = new List<ResaleIndexItemViewModel>();

        public ResaleIndexViewModel(IEnumerable<Offer> offers)
        {
            foreach (var offer in offers)
            {
                Items.Add(new ResaleIndexItemViewModel
                {
                    BookName = offer.BookName,
                    Author = offer.Author,
                    Genre = offer.Genre?.Text ?? string.Empty,
                    Publisher = offer.Publisher?.Text ?? string.Empty,
                    BookType = offer.BookType?.Text ?? string.Empty,
                    ISBN = offer.ISBN,
                    Condition = offer.Condition?.Text ?? string.Empty,
                    Price = offer.BookPrice,
                    OfferStatus = offer.OfferStatus.GetDescription()
                });
            }
        }
    }

    public class ResaleIndexItemViewModel
    {
        public string BookName { get; set; } = null!;

        public string Author { get; set; } = null!;

        public string Genre { get; set; } = null!;

        public string Publisher { get; set; } = null!;

        public string BookType { get; set; } = null!;

        public string ISBN { get; set; } = null!;

        public string Condition { get; set; } = null!;

        public decimal Price { get; set; }

        public string OfferStatus { get; set; } = null!;
    }
}
