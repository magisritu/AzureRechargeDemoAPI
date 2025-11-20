using Azure.Messaging.ServiceBus;

namespace AzureCoreAPI.Services
{
    public class ServiceBusConsumerService : BackgroundService
    {
        private readonly ServiceBusProcessor _processor;

        public ServiceBusConsumerService(ServiceBusClient client, IConfiguration config)
        {
            _processor = client.CreateProcessor(config["AzureServiceBus:QueueName"],
                new ServiceBusProcessorOptions
                {
                    MaxConcurrentCalls = 1,
                    AutoCompleteMessages = false
                });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _processor.ProcessMessageAsync += MessageHandler;
            _processor.ProcessErrorAsync += ErrorHandler;

            await _processor.StartProcessingAsync(stoppingToken);
        }

        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            var body = args.Message.Body.ToString();
            Console.WriteLine($"Received message: {body}");

            // Process message here...

            await args.CompleteMessageAsync(args.Message);
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine($"ERROR: {args.Exception.Message}");
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _processor.CloseAsync();
            await base.StopAsync(cancellationToken);
        }
    }

}
