namespace AzureCoreAPI.Model
{
    public class UpdateWeatherImage
    {
        public required int WeatherId { get; set; }
        public IFormFile? Picture { get; set; }
    }
}
