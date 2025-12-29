using SQLite;

namespace SOSEmergency.Models
{
    [Table("TripRecords")]
    public class TripRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [MaxLength(50), NotNull]
        public string TripId { get; set; } = string.Empty;

        [NotNull]
        public string Latitude { get; set; } = string.Empty;

        [NotNull]
        public string Longitude { get; set; } = string.Empty;

        public string Coordinates => $"{Latitude}, {Longitude}";

        public DateTime CreatedAt { get; set; }

        public string NetworkStatus { get; set; } = string.Empty;
    }
}
