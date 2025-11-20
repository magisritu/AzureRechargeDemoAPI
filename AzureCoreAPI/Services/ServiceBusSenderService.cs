using Azure.Messaging.ServiceBus;
namespace AzureCoreAPI.Services
{

    public interface IServiceBusSender
    {
        Task SendMessageAsync(string message);
        Task<List<string>> PeekMessagesAsync();
    }

    public class ServiceBusSenderService : IServiceBusSender
    {
        private readonly ServiceBusClient _client;
        private readonly IConfiguration _config;

        public ServiceBusSenderService(ServiceBusClient client, IConfiguration config)
        {
            _client = client;
            _config = config;
        }

        public async Task SendMessageAsync(string message)
        {
            string queueName = _config["AzureServiceBus:QueueName"];
            var sender = _client.CreateSender(queueName);

            var sbMessage = new ServiceBusMessage(message)
            {
                ContentType = "application/json"
            };
            try
            {
                await sender.SendMessageAsync(sbMessage);
            }
            catch(Exception e)
            {

            }
        }

        public async Task<List<string>> PeekMessagesAsync()
        {
            var queueName = _config["AzureServiceBus:QueueName"];
            var receiver = _client.CreateReceiver(queueName);

            var messages = await receiver.PeekMessagesAsync(maxMessages: 10);

            return messages.Select(m => m.Body.ToString()).ToList();
        }
    }
}

