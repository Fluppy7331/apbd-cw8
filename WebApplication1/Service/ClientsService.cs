using WebApplication1.Models.DTOs;

namespace WebApplication1.Service;

using Microsoft.Data.SqlClient;

public class ClientsService : IClientsService
{
    private readonly string _connectionString =
        "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";

    public async Task<List<ClientTripDTO>> GetTrip(int clientId)
    {
        var trips = new List<ClientTripDTO>();

        string command = @"
    SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, 
           c.Name AS CountryName, ct.PaymentDate, ct.RegisteredAt
    FROM Client_Trip ct
    INNER JOIN Trip t ON ct.IdTrip = t.IdTrip
    LEFT JOIN Country_Trip tc ON t.IdTrip = tc.IdTrip
    LEFT JOIN Country c ON tc.IdCountry = c.IdCountry
    WHERE ct.IdClient = @clientId";
        
        //to query łączy tabele Client_Trip, Trip, Country_Trip i Country, aby uzyskać szczegóły wycieczek przypisanych do klienta o podanym identyfikatorze.
        //Używamy INNER JOIN, aby uzyskać tylko te wycieczki, które są przypisane do klienta.
        //Używamy LEFT JOIN, aby uwzględnić wycieczki bez przypisanych krajów.
        //Dodaje kraje w podobny sposób jak w metodzie GetTrips, czyli sprawdzam czy już mam informacje o tej wycieczce i dodaje do niej nowy kraj, lub tworze nowa wycieczke i z pierwszym krajem do niej kraj.

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@clientId", clientId);
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                var tripDictionary = new Dictionary<int, ClientTripDTO>();

                while (await reader.ReadAsync())
                {
                    int id = reader.GetInt32(reader.GetOrdinal("IdTrip"));
                    int countryOrdinal = reader.GetOrdinal("CountryName");
                    if (!tripDictionary.TryGetValue(id, out var trip))
                    {
                        int maxOrdinal = reader.GetOrdinal("MaxPeople");
                        trip = new ClientTripDTO()
                        {
                            Id = id,
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                            DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                            MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                            PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate"))
                                ? (int?)null
                                : reader.GetInt32(reader.GetOrdinal("PaymentDate")),
                            RegisteredAt = reader.IsDBNull(reader.GetOrdinal("RegisteredAt"))
                                ? (int?)null
                                : reader.GetInt32(reader.GetOrdinal("RegisteredAt")),
                            Countries = new List<CountryDTO>
                                { new CountryDTO { Name = reader.GetString(countryOrdinal) } },
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

    public async Task<int> CreateClient(ClientDTO client)
    {
        string insertCommand = @"
        INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel);";
        
        //Używamy OUTPUT INSERTED.IdClient, aby zwrócić IdClient nowo dodanego klienta, ponieważ w zadaniu w skrypcie jest nadane IDENTITY dla IdClient.
        //To rozwiazanie pomaga w uniknięciu dodatkowego zapytania SELECT, aby uzyskać IdClient po dodaniu klienta.
        //Jeśli się któryś z warunkow nie jest spelniony rzucamy błąd InvalidOperationException, aby obsłużyć te przypadki w kontrolerze z odpowiednim komentarzem i zwracamy kod błędu.

        int newClientId;

        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            using (SqlCommand insertCmd = new SqlCommand(insertCommand, conn))
            {
                insertCmd.Parameters.AddWithValue("@FirstName", client.FirstName);
                insertCmd.Parameters.AddWithValue("@LastName", client.LastName);
                insertCmd.Parameters.AddWithValue("@Email", client.Email);
                insertCmd.Parameters.AddWithValue("@Telephone", client.Telephone);
                insertCmd.Parameters.AddWithValue("@Pesel", client.Pesel);

                await conn.OpenAsync();
                newClientId = (int)await insertCmd.ExecuteScalarAsync();
            }
        }

        return newClientId;
    }

    public async Task RegisterClientOnTrip(int clientId, int tripId)
    {
        string checkMaxCommand = @"
            SELECT COUNT(1) 
            FROM Client_Trip 
            WHERE IdTrip = @tripId";

        string getMaxPeopleCommand = @"
            SELECT MaxPeople 
            FROM Trip 
            WHERE IdTrip = @tripId";

        string isAlreadyRegisteredCommand = @"
            SELECT COUNT(1)
            FROM Client_Trip
            WHERE IdClient = @clientId AND IdTrip = @tripId";

        string insertCommand = @"
            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate) 
            VALUES (@clientId, @tripId, @registeredAt ,NULL);";

        //Tutaj uzywamy wielu komend SQL, aby sprawdzić czy klient jest już zarejestrowany na wycieczkę, czy nie przekroczono maksymalnej liczby uczestników oraz aby dodać nowego klienta do wycieczki.
        //Używamy transakcji, aby zapewnić, że wszystkie operacje są atomowe i nie zostaną wprowadzone częściowe zmiany w bazie danych.
        //Jeśli którakolwiek z operacji się nie powiedzie, transakcja zostanie wycofana, a zmiany nie zostaną zapisane w bazie danych.
        //Jeśli się któryś z warunkow nie jest spelniony rzucamy błąd InvalidOperationException, aby obsłużyć te przypadki w kontrolerze z odpowiednim komentarzem i zwracamy kod błędu.

        int registeredAt = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            using (SqlTransaction transaction = conn.BeginTransaction())
            using (SqlCommand checkMaxCmd = new SqlCommand(checkMaxCommand, conn, transaction))
            using (SqlCommand getMaxPeopleCmd = new SqlCommand(getMaxPeopleCommand, conn, transaction))
            using (SqlCommand isAlreadyRegisteredCmd = new SqlCommand(isAlreadyRegisteredCommand, conn, transaction))
            using (SqlCommand insertCmd = new SqlCommand(insertCommand, conn, transaction))
            {
                checkMaxCmd.Parameters.AddWithValue("@tripId", tripId);
                getMaxPeopleCmd.Parameters.AddWithValue("@tripId", tripId);
                isAlreadyRegisteredCmd.Parameters.AddWithValue("@clientId", clientId);
                isAlreadyRegisteredCmd.Parameters.AddWithValue("@tripId", tripId);

                int currentParticipants = (int)await checkMaxCmd.ExecuteScalarAsync();

                int maxPeople = (int)await getMaxPeopleCmd.ExecuteScalarAsync();

                int isAlreadyRegistered = (int)await isAlreadyRegisteredCmd.ExecuteScalarAsync();


                if (isAlreadyRegistered == 1)
                {
                    transaction.Rollback();
                    throw new InvalidOperationException("Ten klient jest już zarejestrowany na tę wycieczkę.");
                }

                if (currentParticipants >= maxPeople)
                {
                    transaction.Rollback();
                    throw new InvalidOperationException("Osiągnięto maksymalną liczbę uczestników dla tej wycieczki.");
                }
                
                insertCmd.Parameters.AddWithValue("@clientId", clientId);
                insertCmd.Parameters.AddWithValue("@tripId", tripId);
                insertCmd.Parameters.AddWithValue("@registeredAt", registeredAt);
                await insertCmd.ExecuteNonQueryAsync();

                transaction.Commit();
            }
        }
    }

    public async Task UnregisterClientFromTrip(int clientId, int tripId)
    {
        string isAlreadyRegisteredCommand = @"
        SELECT 1 FROM Client_Trip
        WHERE IdClient = @clientId AND IdTrip = @tripId";

        string deleteCommand = @"
        DELETE FROM Client_Trip WHERE IdClient = @clientId AND IdTrip = @tripId";
        
        //Tutaj używamy transakcji, aby zapewnić, że operacja usunięcia klienta z wycieczki jest atomowa podobnie jak w metodzie RegisterClientOnTrip.
        //Jeśli klient nie jest zarejestrowany na wycieczkę, transakcja zostanie wycofana, a zmiany nie zostaną zapisane w bazie danych.
        //Tutaj podobnie jeśli się któryś z warunkow nie jest spelniony rzucamy błąd InvalidOperationException, aby obsłużyć te przypadki w kontrolerze z odpowiednim komentarzem i zwracamy kod błędu.
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            await conn.OpenAsync();
            using (SqlTransaction transaction = conn.BeginTransaction())
            using (SqlCommand isAlreadyRegisteredCmd = new SqlCommand(isAlreadyRegisteredCommand, conn, transaction))
            using (SqlCommand deleteCmd = new SqlCommand(deleteCommand, conn, transaction))
            {
                    isAlreadyRegisteredCmd.Parameters.AddWithValue("@clientId", clientId);
                    isAlreadyRegisteredCmd.Parameters.AddWithValue("@tripId", tripId);
                    deleteCmd.Parameters.AddWithValue("@clientId", clientId);
                    deleteCmd.Parameters.AddWithValue("@tripId", tripId);

                    var isAlreadyRegistered = await isAlreadyRegisteredCmd.ExecuteScalarAsync();

                    if (isAlreadyRegistered == null)
                    {
                        transaction.Rollback();
                        throw new InvalidOperationException("Klient nie jest zarejestrowany na tej wycieczce");
                    }

                    await deleteCmd.ExecuteNonQueryAsync();
                    transaction.Commit();
            }
        }
    }


    public async Task<Boolean> DoesClientExist(int idClient)
    {
        string command = "SELECT COUNT(1) FROM Client WHERE IdClient = @idClient;";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@idClient", idClient); 

            await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            return (result != null && (int)result > 0);
        }
    }

    public async Task<Boolean> DoesTripExist(int id)
    {
        string command = "SELECT COUNT(1) FROM dbo.Trip WHERE IdTrip = @id;";

        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            cmd.Parameters.AddWithValue("@id", id); 

            await conn.OpenAsync();

            var result = await cmd.ExecuteScalarAsync();
            return (result != null && (int)result > 0);
        }
    }

    public async Task<Boolean> ValidateClient(ClientDTO client)
    {
        if (string.IsNullOrWhiteSpace(client.FirstName) ||
            string.IsNullOrWhiteSpace(client.LastName) ||
            string.IsNullOrWhiteSpace(client.Email) ||
            string.IsNullOrWhiteSpace(client.Telephone) ||
            string.IsNullOrWhiteSpace(client.Pesel))
        {
            return false;
        }

        if (!client.Telephone.StartsWith("+") || client.Telephone.Length < 10 || client.Telephone.Length > 15)
        {
            return false;
        }

        if (client.Pesel.Length != 11 || !long.TryParse(client.Pesel, out _))
        {
            return false;
        }

        if (!client.Email.Contains("@") || !client.Email.Contains("."))
        {
            return false;
        }

        return true; // Zwróć true, jeśli klient jest poprawny, w przeciwnym razie false
    }
}