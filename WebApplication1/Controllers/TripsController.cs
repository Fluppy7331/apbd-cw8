using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Model;
using WebApplication1.Service;

namespace WebApplication1
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly ITripsService _tripsService;

        public TripsController(ITripsService tripsService)
        {
            _tripsService = tripsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTrips()
        {
            try
            {
                var trips = await _tripsService.GetTrips();
                return Ok(trips);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Wystąpił błąd serwera.");
            }
        }
        
    }
}