using SQLite;
using SOSEmergency.Models;

namespace SOSEmergency.Services
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection? _database;

        private async Task InitializeAsync()
        {
            if (_database != null)
                return;

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "sosemergency.db3");
            _database = new SQLiteAsyncConnection(dbPath);
            await _database.CreateTableAsync<TripRecord>();
        }

        public async Task<int> SaveTripRecordAsync(TripRecord tripRecord)
        {
            await InitializeAsync();
            tripRecord.CreatedAt = DateTime.Now;
            return await _database!.InsertAsync(tripRecord);
        }

        public async Task<List<TripRecord>> GetAllTripRecordsAsync()
        {
            await InitializeAsync();
            return await _database!.Table<TripRecord>().ToListAsync();
        }

        public async Task<TripRecord?> GetTripRecordAsync(int id)
        {
            await InitializeAsync();
            return await _database!.Table<TripRecord>()
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> UpdateTripRecordAsync(TripRecord tripRecord)
        {
            await InitializeAsync();
            return await _database!.UpdateAsync(tripRecord);
        }

        public async Task<int> DeleteTripRecordAsync(TripRecord tripRecord)
        {
            await InitializeAsync();
            return await _database!.DeleteAsync(tripRecord);
        }

        public async Task<int> GetRecordCountAsync()
        {
            await InitializeAsync();
            return await _database!.Table<TripRecord>().CountAsync();
        }
    }
}
