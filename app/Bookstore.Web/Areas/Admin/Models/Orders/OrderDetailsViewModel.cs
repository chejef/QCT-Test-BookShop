using Bookstore.Domain.Orders;

namespace Bookstore.Web.Areas.Admin.Models.Orders
{
    public class OrderDetailsViewModel
    {
        public int OrderId { get; set; }

        public OrderStatus SelectedOrderStatus { get; set; }

        public DateTimeOffset OrderDate { get; set; }

        public DateTimeOffset DeliveryDate { get; set; }

        public string CustomerName { get; set; } = null!;

        public string AddressLine1 { get; set; } = null!;

        public string AddressLine2 { get; set; } = null!;

        public string City { get; set; } = null!;

        public string State { get; set; } = null!;

        public string ZipCode { get; set; } = null!;

        public string Country { get; set; } = null!;

        public decimal Subtotal { get; set; }

        public decimal Tax { get; set; }

        public decimal Total => Subtotal + Tax;

        public List<OrderDetailsItemViewModel> Items { get; set; } = new List<OrderDetailsItemViewModel>();

        public OrderDetailsViewModel() { }

        public OrderDetailsViewModel(Order order)
        {
            OrderId = order.Id;
            CustomerName = order.Customer?.FullName ?? string.Empty;
            SelectedOrderStatus = order.OrderStatus;
            AddressLine1 = order.Address?.AddressLine1 ?? string.Empty;
            AddressLine2 = order.Address?.AddressLine2 ?? string.Empty;
            City = order.Address?.City ?? string.Empty;
            State = order.Address?.State ?? string.Empty;
            ZipCode = order.Address?.ZipCode ?? string.Empty;
            Country = order.Address?.Country ?? string.Empty;
            Subtotal = order.SubTotal;
            Tax = order.Tax;
            OrderDate = order.CreatedOn;
            DeliveryDate = order.DeliveryDate;

            foreach (var orderItem in order.OrderItems)
            {
                Items.Add(new OrderDetailsItemViewModel
                {
                    Author = orderItem.Book?.Author ?? string.Empty,
                    BookType = orderItem.Book?.BookType?.Text ?? string.Empty,
                    Condition = orderItem.Book?.Condition?.Text ?? string.Empty,
                    Genre = orderItem.Book?.Genre?.Text ?? string.Empty,
                    Name = orderItem.Book?.Name ?? string.Empty,
                    Price = orderItem.Book?.Price ?? 0,
                    Publisher = orderItem.Book?.Publisher?.Text ?? string.Empty
                });
            }
        }
    }

    public class OrderDetailsItemViewModel
    {
        public string Name { get; set; } = null!;

        public string Author { get; set; } = null!;

        public string Publisher { get; set; } = null!;

        public string Genre { get; set; } = null!;

        public string BookType { get; set; } = null!;

        public string Condition { get; set; } = null!;

        public decimal Price { get; set; }
    }
}