using Microsoft.Data.SqlClient;
using WebApplication1.Models.DTOs;

namespace WebApplication1.Service;

using Microsoft.Data.SqlClient;
using System.Data;

public class TripsService : ITripsService
{
    private readonly string _connectionString =
        "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";

    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

        string command = @"
        SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName
        FROM Trip t
        LEFT JOIN Country_Trip tc ON t.IdTrip = tc.IdTrip
        LEFT JOIN Country c ON tc.IdCountry = c.IdCountry";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                var tripDictionary = new Dictionary<int, TripDTO>();

                while (await reader.ReadAsync())
                {
                    int id = reader.GetInt32(reader.GetOrdinal("IdTrip"));
                    int countryOrdinal = reader.GetOrdinal("CountryName");
                    if (!tripDictionary.TryGetValue(id, out var trip))
                    {
                        int maxOrdinal = reader.GetOrdinal("MaxPeople");
                        trip = new TripDTO()
                        {
                            Id = id,
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                            DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                            MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                            Countries = new List<CountryDTO> { new CountryDTO { Name = reader.GetString(countryOrdinal) } },
                        };
                        tripDictionary.Add(id, trip);
                    }
                    else
                    {
                        trip.Countries.Add(new CountryDTO()
                        {
                            Name = reader.GetString(countryOrdinal)
                        });
                    }
                }
                trips = tripDictionary.Values.ToList();
            }
        }

        return trips;
    }
    
    public async Task<Boolean> DoesTripExist(int id)
    {
        string command = "SELECT COUNT(1) FROM dbo.Trip WHERE IdTrip = @id;";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@id", id); // Dodanie parametru @id

            await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            return (result != null && (int)result > 0);
        }
    }
}