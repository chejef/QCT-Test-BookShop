using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bookstore.Domain.ReferenceData
{
    public interface IReferenceDataService
    {
        Task<IPagedResult<ReferenceDataItem>> GetReferenceDataAsync(ReferenceDataFilters filters, int pageIndex, int pageSize);

        Task<IEnumerable<ReferenceDataItem>> GetAllReferenceDataAsync();

        Task<ReferenceDataItem?> GetReferenceDataItemAsync(int id);

        Task CreateAsync(CreateReferenceDataItemDto createReferenceDataItemDto);

        Task UpdateAsync(UpdateReferenceDataItemDto createReferenceDataItemDto);
    }

    public class ReferenceDataService : IReferenceDataService
    {
        private readonly IReferenceDataRepository referenceDataRepository;

        public ReferenceDataService(IReferenceDataRepository referenceDataRepository)
        {
            this.referenceDataRepository = referenceDataRepository;
        }

        public async Task<IPagedResult<ReferenceDataItem>> GetReferenceDataAsync(ReferenceDataFilters filters, int pageIndex, int pageSize)
        {
            return await referenceDataRepository.ListAsync(filters, pageIndex, pageSize);
        }

        public async Task<IEnumerable<ReferenceDataItem>> GetAllReferenceDataAsync()
        {
            return await referenceDataRepository.FullListAsync();
        }

        public async Task<ReferenceDataItem?> GetReferenceDataItemAsync(int id)
        {
            return await referenceDataRepository.GetAsync(id);
        }

        public async Task CreateAsync(CreateReferenceDataItemDto dto)
        {
            var referenceDataItem = new ReferenceDataItem(dto.ReferenceDataType, dto.Text);

            await referenceDataRepository.AddAsync(referenceDataItem);

            await referenceDataRepository.SaveChangesAsync();
        }

        public async Task UpdateAsync(UpdateReferenceDataItemDto dto)
        {
            var referenceDataItem = await referenceDataRepository.GetAsync(dto.Id)
                ?? throw new InvalidOperationException($"Reference data item with id '{dto.Id}' not found.");

            referenceDataItem.DataType = dto.ReferenceDataType;
            referenceDataItem.Text = dto.Text;

            await referenceDataRepository.SaveChangesAsync();
        }
    }
}