using Bookstore.Domain.Orders;

namespace Bookstore.Web.ViewModel.Checkout
{
    public class CheckoutFinishedViewModel
    {
        public IEnumerable<CheckoutFinishedItemViewModel> Items { get; set; } = new List<CheckoutFinishedItemViewModel>();

        public CheckoutFinishedViewModel(Order order)
        {
            Items = order.OrderItems.Select(x => new CheckoutFinishedItemViewModel
            {
                BookId = x.Book?.Id ?? 0,
                Bookname = x.Book?.Name ?? string.Empty,
                Price = x.Book?.Price ?? 0,
                Quantity = x.Quantity,
                Url = x.Book?.CoverImageUrl ?? string.Empty
            });
        }
    }

    public class CheckoutFinishedItemViewModel
    {
        public string Bookname { get; set; } = null!;

        public long BookId { get; set; }

        public int Quantity { get; set; }

        public string Url { get; set; } = null!;

        public decimal Price { get; set; }
    }
}
