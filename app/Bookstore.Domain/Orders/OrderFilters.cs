using System;

namespace Bookstore.Domain.Orders
{
    public class OrderFilters
    {
        public OrderStatus? OrderStatusFilter { get; set; }

        public DateTimeOffset? OrderDateFromFilter { get; set; }

        public DateTimeOffset? OrderDateToFilter { get; set; }
    }
}