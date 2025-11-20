using AzureCoreAPI.Model;
using Microsoft.Azure.Cosmos;

namespace AzureCoreAPI.Services
{
    public interface ICosmosDbService
    {
        Task AddItemAsync<T>(T item);
        Task<T> GetItemAsync<T>(string id, string partitionKey);
        Task<List<PaymentRequest>> GetByPhoneNumberAsync(string phone);
    }

    public class CosmosDbService : ICosmosDbService
    {
        private readonly Container _container;

        public CosmosDbService(CosmosClient client, IConfiguration config)
        {
            var dbName = config["CosmosDb:DatabaseName"];
            var containerName = config["CosmosDb:ContainerName"];
            _container = client.GetContainer(dbName, containerName);
        }

        public async Task AddItemAsync<T>(T item)
        {
            await _container.CreateItemAsync(item);
        }

        public async Task<T> GetItemAsync<T>(string id, string partitionKey)
        {
            try
            {
                var response = await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return default;
            }
        }

        public async Task<List<PaymentRequest>> GetByPhoneNumberAsync(string phone)
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.phoneNumber = @phone")
                    .WithParameter("@phone", phone);

            var results = new List<PaymentRequest>();

            using (FeedIterator<PaymentRequest> iterator =
                   _container.GetItemQueryIterator<PaymentRequest>(query))
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    results.AddRange(response);
                }
            }

            return results;
        }

    }

}

