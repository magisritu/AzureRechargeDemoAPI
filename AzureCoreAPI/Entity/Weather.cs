using System.ComponentModel.DataAnnotations;

namespace AzureCoreAPI.Entity
{
    public class Weather
    {
        [Key]
        public int ID { get; set; }
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF { get; set; }

        [MaxLength(100)]
        public string? Summary { get; set; }
        public string? WeatherImageUrl { get; set; }
    }
}
