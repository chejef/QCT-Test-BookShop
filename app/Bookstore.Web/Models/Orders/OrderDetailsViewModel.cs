using Bookstore.Domain;
using Bookstore.Domain.Orders;

namespace Bookstore.Web.ViewModel.Orders
{
    public class OrderDetailsViewModel
    {
        public int OrderId { get; set; }

        public string OrderStatus { get; set; } = null!;

        public DateTimeOffset DeliveryDate { get; set; }

        public decimal Total { get; set; }

        public List<OrderDetailsItemViewModel> OrderItems { get; set; } = new List<OrderDetailsItemViewModel>();

        public OrderDetailsViewModel(Order order)
        {
            OrderId = order.Id;
            DeliveryDate = order.DeliveryDate;
            OrderStatus = order.OrderStatus.GetDescription();
            Total = order.Total;

            OrderItems = order.OrderItems.Select(x => new OrderDetailsItemViewModel
            {
                BookId = x.BookId,
                BookName = x.Book?.Name ?? string.Empty,
                ImageUrl = x.Book?.CoverImageUrl ?? string.Empty,
                Price = x.Book?.Price ?? 0
            }).ToList();
        }
    }

    public class OrderDetailsItemViewModel
    {
        public int BookId { get; set; }

        public string ImageUrl { get; set; } = null!;

        public string BookName { get; set; } = null!;

        public decimal Price { get; set; }
    }
}