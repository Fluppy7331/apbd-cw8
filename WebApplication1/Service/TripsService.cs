using Microsoft.Data.SqlClient;
using WebApplication1.Models.DTOs;

namespace WebApplication1.Service;

public class TripsService : ITripsService
{
    
    private readonly string _connectionString= "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD-cw8;Integrated Security=True;";
    
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();
        
        string sql = "select Id, Name from Trip";

        using (SqlConnection connection = new SqlConnection(_connectionString))
        using (SqlCommand command = new SqlCommand(sql, connection))
        {
            await connection.OpenAsync();

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int intOrdinal = reader.GetOrdinal("Id");
                    trips.Add(new TripDTO()
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                    });
                }
            }

        }
        return trips;
            
    }
    
}