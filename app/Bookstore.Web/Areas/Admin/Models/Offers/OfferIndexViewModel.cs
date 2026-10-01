using Bookstore.Domain;
using Bookstore.Domain.Offers;
using Bookstore.Domain.ReferenceData;
using Bookstore.Web.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace Bookstore.Web.Areas.Admin.Models.Offers
{
    public class OfferIndexViewModel : PaginatedViewModel
    {
        public OfferIndexViewModel(IPagedResult<Offer> offers, IEnumerable<ReferenceDataItem> referenceData)
        {
            foreach (var offer in offers)
            {
                Items.Add(new OfferIndexItemViewModel
                {
                    OfferId = offer.Id,
                    BookName = offer.BookName ?? string.Empty,
                    Author = offer.Author ?? string.Empty,
                    Genre = offer.Genre?.Text ?? string.Empty,
                    CustomerName = offer.Customer?.FullName ?? string.Empty,
                    OfferStatus = offer.OfferStatus,
                    OfferDate = offer.CreatedOn,
                    OfferPrice = offer.BookPrice,
                    Condition = offer.Condition?.Text ?? string.Empty
                });
            }

            PageIndex = offers.PageIndex;
            PageSize = offers.Count;
            PageCount = offers.TotalPages;
            HasNextPage = offers.HasNextPage;
            HasPreviousPage = offers.HasPreviousPage;
            PaginationButtons = offers.GetPageList(5).ToList();

            Genres = referenceData.Where(x => x.DataType == ReferenceDataType.Genre).Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Text });
            BookConditions = referenceData.Where(x => x.DataType == ReferenceDataType.Condition).Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Text });
        }

        public List<OfferIndexItemViewModel> Items { get; set; } = new List<OfferIndexItemViewModel>();

        public OfferFilters Filters { get; set; } = new OfferFilters();

        public IEnumerable<SelectListItem> Genres { get; set; } = new List<SelectListItem>();

        public IEnumerable<SelectListItem> BookConditions { get; set; } = new List<SelectListItem>();
    }

    public class OfferIndexItemViewModel
    {
        public int OfferId { get; set; }

        public string BookName { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public string Genre { get; set; } = string.Empty;

        public OfferStatus OfferStatus { get; set; }

        public DateTimeOffset OfferDate { get; internal set; }

        public decimal OfferPrice { get; internal set; }

        public string Condition { get; internal set; } = string.Empty;
    }
}
