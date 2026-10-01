using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Bookstore.Domain.Books;

namespace Bookstore.Web.ViewModel.Search
{
    public class SearchDetailsViewModel
    {
        public int BookId { get; set; }

        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        [Display(Name = "Title")]
        [DefaultValue("Title")]
        public string BookName { get; set; } = string.Empty;

        [DefaultValue("Publisher not found")] public string PublisherName { get; set; } = string.Empty;

        [DefaultValue("No Author")] public string Author { get; set; } = string.Empty;

        public string ISBN { get; set; } = string.Empty;

        [Display(Name = "Genre")] public string GenreName { get; set; } = string.Empty;

        [Display(Name = "Type")] public string TypeName { get; set; } = string.Empty;

        [Display(Name = "Condition")] public string ConditionName { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        [Display(Name = "$$")] public decimal MinPrice { get; set; }

        public int Quantity { get; set; }

        public string Summary { get; set; } = string.Empty;

        public SearchDetailsViewModel(Book book)
        {
            BookName = book.Name ?? string.Empty;
            Author = book.Author ?? string.Empty;
            PublisherName = book.Publisher?.Text ?? string.Empty;
            ISBN = book.ISBN ?? string.Empty;
            GenreName = book.Genre?.Text ?? string.Empty;
            TypeName = book.BookType?.Text ?? string.Empty;
            ConditionName = book.Condition?.Text ?? string.Empty;
            Url = book.CoverImageUrl ?? string.Empty;
            MinPrice = book.Price;
            Quantity = book.Quantity;
            BookId = book.Id;
            Summary = book.Summary ?? string.Empty;
        }
    }
}
