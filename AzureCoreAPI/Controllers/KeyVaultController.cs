using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using AzureCoreAPI.Repository;

namespace AzureCoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KeyVaultController : ControllerBase
    {
        private readonly SecretClient _secretClient;
        private const string SecretName = "DatabaseConnectionString";
        private const string KeyVaultUrl = "https://riturajkv.vault.azure.net/";

        private readonly IWeatherRepository _weatherRepository;

        public KeyVaultController(IWeatherRepository weatherRepository)
        {
            // Using Managed Identity to authenticate with Key Vault
            _secretClient = new SecretClient(new Uri(KeyVaultUrl), new DefaultAzureCredential());
            _weatherRepository = weatherRepository;
        }

        [HttpGet("Login/{email}/{password}")]
        public async Task<IActionResult> Login(string email, string password)
        {
            try
            {
                string CLIENT_ID = "017529ba-a0d1-4944-b1ff-7148e175e4b6";
                string BASE_URI = "https://riturajkv1.vault.azure.net/";
                string CLIENT_SECRET = "fvI8Q~IqNDyoXGtWjUcO~Feyt5MgA2XJOco3XbP";


                var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(
                    async (string auth, string res, string scope) =>
                    {
                        var authContext = new AuthenticationContext(auth);
                        var credential = new ClientCredential(CLIENT_ID, CLIENT_SECRET);
                        AuthenticationResult result = await authContext.AcquireTokenAsync(res, credential);
                        if (result == null)
                        {
                            throw new InvalidOperationException("failed to retrieve token");
                        }
                        return result.AccessToken;
                    }
                ));
                var secretData = await client.GetSecretAsync(BASE_URI, "DatabaseConnectionString");

                if(_weatherRepository.Login(email, password))
                {
                    // var secret = await _secretClient.GetSecretAsync(SecretName);
                    return Ok(new { SecretName, SecretValue = secretData.Value });
                }
                else
                {
                    return BadRequest("User doesn't exist");
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Error accessing Key Vault", Error = ex.Message });
            }
        }
    }
}
