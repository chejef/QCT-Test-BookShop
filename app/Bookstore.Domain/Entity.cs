using System;
using System.ComponentModel.DataAnnotations;

namespace Bookstore.Domain
{
    public abstract class Entity
    {
        public int Id { get; set; }

        public string CreatedBy { get; set; } = "System";

        public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset UpdatedOn { get; set; } = DateTimeOffset.UtcNow;

        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        public bool IsNewEntity()
        {
            return Id == 0;
        }
    }
}