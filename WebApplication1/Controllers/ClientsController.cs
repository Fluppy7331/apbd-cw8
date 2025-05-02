using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.DTOs;
using WebApplication1.Service;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IClientsService _clientsService;

        public ClientsController(IClientsService clientsService)
        {
            _clientsService = clientsService;
        }

        [HttpGet("{clientId}/trips")]
        public async Task<IActionResult> GetTrip(int clientId)
        {
            try
            {
                if (clientId <= 0)
                {
                    return BadRequest("Identyfikator klienta musi być większy od zera.");
                }


                if (!await _clientsService.DoesClientExist(clientId))
                {
                    return NotFound($"Klient o identyfikatorze {clientId} nie istnieje.");
                }

                var trip = await _clientsService.GetTrip(clientId);

                if (!trip.Any())
                {
                    return NotFound($"Klient o identyfikatorze {clientId} nie ma przypisanych wycieczek.");
                }

                return Ok(trip);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd serwera.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] ClientDTO client)
        {
            try
            {
                if (client == null)
                {
                    return BadRequest("Nieprawidłowe dane klienta.");
                }

                if (!await _clientsService.ValidateClient(client))
                {
                    return BadRequest("Dane klienta nie przeszły podstawowej walidacji.");
                }

                var newClientId = await _clientsService.CreateClient(client);
                if (newClientId <= 0)
                {
                    return BadRequest("Nie udało się utworzyć klienta.");
                }

                return CreatedAtAction(nameof(CreateClient), new { clientId = newClientId }, new { id = newClientId });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd serwera.");
            }
        }

        [HttpPut("{clientId}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClientOnTrip(int clientId, int tripId)
        {
            try
            {
                if (clientId <= 0 || tripId <= 0)
                {
                    return BadRequest("Identyfikatory klienta i wycieczki muszą być większe od zera.");
                }

                if (!await _clientsService.DoesClientExist(clientId))
                {
                    return NotFound($"Klient o identyfikatorze {clientId} nie istnieje.");
                }

                if (!await _clientsService.DoesTripExist(tripId))
                {
                    return NotFound($"Wycieczka o identyfikatorze {tripId} nie istnieje.");
                }

                try
                {
                    await _clientsService.RegisterClientOnTrip(clientId, tripId);
                }
                // Maksymalna ilość klientów na wycieczkę została obłużona w środku RegisterClientOnTrip, a przekroczenie jej zwracany jest jako InvalidOperationException
                // ten wyjatek obsługuje dodatkowo przypadek jak klient juz jest zarejestrowany na wycieczkę
                catch (InvalidOperationException e)
                {
                    return BadRequest(e.Message);
                }
                return Ok("Klient został pomyślnie zarejestrowany na wycieczkę.");
                
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd serwera.");
            }
        }
        
        [HttpDelete("{clientId}/trips/{tripId}")]
        public async Task<IActionResult> UnregisterClientFromTrip(int clientId, int tripId)
        {
            try
            {
                if (clientId <= 0 || tripId <= 0)
                {
                    return BadRequest("Identyfikatory klienta i wycieczki muszą być większe od zera.");
                }

                if (!await _clientsService.DoesClientExist(clientId))
                {
                    return NotFound($"Klient o identyfikatorze {clientId} nie istnieje.");
                }

                if (!await _clientsService.DoesTripExist(tripId))
                {
                    return NotFound($"Wycieczka o identyfikatorze {tripId} nie istnieje.");
                }

                try
                {
                    await _clientsService.UnregisterClientFromTrip(clientId, tripId);
                }
                //Tutaj zostało zrobiony w podobny soposób sposób jak w RegisterClientOnTrip, czyli jeśli klient nie jest zarejestrowany to zrollbackuje transakcje oraz
                //rzuci poniższy błąd
                catch(InvalidOperationException e)
                {
                    return BadRequest(e.Message);
                }
                return Ok("Klient został pomyślnie wyrejestrowany z wycieczki.");
                
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd serwera.");
            }
        }
    }
}