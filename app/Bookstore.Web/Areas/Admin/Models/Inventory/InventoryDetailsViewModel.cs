using Bookstore.Domain.Books;

namespace Bookstore.Web.Areas.Admin.Models.Inventory
{
    public class InventoryDetailsViewModel
    {
        public InventoryDetailsViewModel() { }

        public InventoryDetailsViewModel(Book book)
        {
            Author = book.Author ?? string.Empty;
            BookType = book.BookType?.Text ?? string.Empty;
            Condition = book.Condition?.Text ?? string.Empty;
            CoverImageUrl = book.CoverImageUrl ?? string.Empty;
            Genre = book.Genre?.Text ?? string.Empty;
            Id = book.Id;
            ISBN = book.ISBN ?? string.Empty;
            Name = book.Name ?? string.Empty;
            Price = book.Price;
            Publisher = book.Publisher?.Text ?? string.Empty;
            Quantity = book.Quantity;
            Summary = book.Summary ?? string.Empty;
            Year = book.Year.GetValueOrDefault();
        }

        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public int Year { get; set; }

        public string Publisher { get; set; } = string.Empty;

        public string BookType { get; set; } = string.Empty;

        public string Genre { get; set; } = string.Empty;

        public string Condition { get; set; } = string.Empty;

        public string CoverImageUrl { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string ISBN { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; }
    }
}
