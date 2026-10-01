using Bookstore.Domain;
using Bookstore.Domain.Orders;
using Bookstore.Web.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bookstore.Web.Areas.Admin.Models.Orders
{
    public class OrderIndexViewModel : PaginatedViewModel
    {
        public List<OrderIndexListItemViewModel> Items { get; set; } = new List<OrderIndexListItemViewModel>();

        public OrderFilters Filters { get; set; } = new OrderFilters();

        public OrderIndexViewModel(IPagedResult<Order> orderDtos, OrderFilters filters)
        {
            foreach (var order in orderDtos)
            {
                Items.Add(new OrderIndexListItemViewModel
                {
                    Id = order.Id,
                    CustomerName = order.Customer?.FullName ?? string.Empty,
                    OrderStatus = order.OrderStatus,
                    OrderDate = order.CreatedOn,
                    DeliveryDate = order.DeliveryDate,
                    Total = order.Total
                });
            }

            Filters = filters;

            PageIndex = orderDtos.PageIndex;
            PageSize = orderDtos.Count;
            PageCount = orderDtos.TotalPages;
            HasNextPage = orderDtos.HasNextPage;
            HasPreviousPage = orderDtos.HasPreviousPage;
            PaginationButtons = orderDtos.GetPageList(5).ToList();
        }
    }

    public class OrderIndexListItemViewModel
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public OrderStatus OrderStatus { get; set; }

        public DateTime DeliveryDate { get; set; }

        public DateTimeOffset OrderDate { get; internal set; }

        public decimal Total { get; internal set; }
    }
}