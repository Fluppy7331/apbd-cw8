using WebApplication1.Models.DTOs;

namespace WebApplication1.Service;

public interface ITripsService
{
    public Task<List<TripDTO>> GetTrips();
    public Task<Boolean> DoesTripExist(int id);

}