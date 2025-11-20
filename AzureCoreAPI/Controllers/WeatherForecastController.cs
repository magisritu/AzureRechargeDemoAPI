using Azure.Storage.Blobs;
using AzureCoreAPI.Entity;
using AzureCoreAPI.Model;
using AzureCoreAPI.Repository;
using AzureCoreAPI.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Web.Resource;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AzureCoreAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IWeatherRepository _weatherRepository;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
            IWeatherRepository weatherRepository,
            IAzureBlobStorageService blobStorageService,
            IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _weatherRepository = weatherRepository;
            _blobStorageService = blobStorageService;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        [RequiredScope(RequiredScopesConfigurationKey = "AzureAd:Scopes:Read")]
        public async Task<IEnumerable<Weather>> Get()
        {
            return await _weatherRepository.GetWeathers();
        }

        [HttpPost(Name = "AddWeatherForecast")]
        public async Task<IEnumerable<Weather>> Post(Weather weather)
        {
            return await _weatherRepository.AddWeather(weather);
        }

        [HttpPut(Name = "UpdateWeatherForecast")]
        public async Task<IEnumerable<Weather>> Put(int id, Weather weather)
        {
            return await _weatherRepository.UpdateWeather(weather);
        }

        [HttpPost("UpdateWeatherImage")]
        public async Task<IActionResult> UpdateWeatherImage([FromForm] UpdateWeatherImage model)
        {
            //string pictureUrl = null;
            // Test commit 3222
            var functionUrl = _config.GetSection("AzureFunction").GetSection("WeatherImageUpload").Value;
            // Call Azure fucntion to upload image

            if (model.Picture == null || model.Picture.Length == 0)
                return BadRequest("No file uploaded");

            // Step 1: Forward file to Azure Function
            try
            {
                _logger.LogWarning("function call started");
                using var httpClient = _httpClientFactory.CreateClient();

                using var formData = new MultipartFormDataContent();

                // Add Weather JSON
                var weatherJson = JsonSerializer.Serialize(model.WeatherId);
                formData.Add(new StringContent(weatherJson), "weatherId");

                using var fileStream = model.Picture.OpenReadStream();
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(model.Picture.ContentType);

                formData.Add(fileContent, "Picture", model.Picture.FileName);

                var response = await httpClient.PostAsync(functionUrl, formData);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Azure Function upload failed: {response.StatusCode}");
                    return StatusCode((int)response.StatusCode, "Upload failed in Azure Function");
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var uploadResult = JsonSerializer.Deserialize<UploadResult>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (uploadResult == null || string.IsNullOrEmpty(uploadResult.PictureUrl))
                    return StatusCode(500, "Failed to get image URL from Azure Function");

                // Step 2: Update Weather record with image URL
                await _weatherRepository.UpdateWeatherImage(model.WeatherId, uploadResult.PictureUrl);
                _logger.LogWarning("completed and url saved on database");
                return Ok(new { weatherId = model.WeatherId, imageUrl = uploadResult.PictureUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while uploading image");
                return StatusCode(500, "Internal server error");
            }

            //if (model.Picture != null)
            //{
            //    using (var stream = new MemoryStream())
            //    {
            //        await model.Picture.CopyToAsync(stream);

            //        // Upload the byte array or stream to Azure Blob Storage
            //        pictureUrl = await _blobStorageService.UploadAsync(stream.ToArray(),
            //            $"{model.WeatherId}_weather_image.{model.Picture.FileName.Split('.').LastOrDefault()}");
            //    }

            //    // Update the profile picture URL in the database
            //    await _weatherRepository.UpdateWeatherImage(model.WeatherId, pictureUrl);
            //}
        }

        [HttpGet("DownloadImage/{id}")]
        public async Task<IActionResult> DownloadImage(int id)
        {
            // Test commit message
            var weather = await _weatherRepository.GetWeathersById(id);
            if(weather.WeatherImageUrl == null)
            {
                 return StatusCode(500, "Failed to get image URL");
            }
            string connectionString = _config.GetSection("AzureBlobStorage").GetSection("ConnectionString").Value;
            string containerName = "images";
            string fileName = weather.WeatherImageUrl.Split("/")[weather.WeatherImageUrl.Split("/").Length - 1];
            var blobClient = new BlobClient(connectionString, containerName, fileName);
            var bb = !await blobClient.ExistsAsync();
            if (!await blobClient.ExistsAsync())
                return NotFound("File not found");

            var blob = await blobClient.DownloadContentAsync();
            var stream = blob.Value.Content.ToStream();
            var contentType = blob.Value.Details.ContentType ?? "application/octet-stream";

            return File(stream, contentType, fileName);
        }
    }

}
