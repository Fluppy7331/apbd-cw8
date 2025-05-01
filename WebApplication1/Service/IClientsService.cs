using WebApplication1.Models.DTOs;

namespace WebApplication1.Service;


public interface IClientsService
{
    public Task<List<ClientTripDTO>> GetTrip(int clientId);
    public Task<int> CreateClient(ClientDTO client);
    public Task RegisterClientOnTrip(int clientId, int tripId);
    public Task UnregisterClientFromTrip(int clientId, int tripId);
    public Task<Boolean> DoesTripExist(int id);
    public Task<Boolean> DoesClientExist(int id);
    public Task<Boolean> ValidateClient(ClientDTO client);

}