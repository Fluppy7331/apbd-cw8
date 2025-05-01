using System.Text.Json.Serialization;

namespace WebApplication1.Models.DTOs;

public class TripDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public int MaxPeople { get; set; }
    public List<CountryDTO> Countries { get; set; }
    
}
public class ClientTripDTO : TripDTO
{
    [JsonPropertyOrder(1)]
    public int? PaymentDate { get; set; } 
    [JsonPropertyOrder(2)]
    public int? RegisteredAt { get; set; } 
}

public class CountryDTO
{
    public string Name { get; set; }
}

