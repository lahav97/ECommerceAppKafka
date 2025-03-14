using Confluent.Kafka;
using Newtonsoft.Json;
using CartService.Models;

namespace CartService
{
    public class Publisher
    { 
        private readonly IProducer<string, string> _producer;
        private readonly string _topicName = "ordersTopic";
        private readonly string _BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BROKER") ?? "kafka:9092";
        private readonly int _maxRetries = 5;

        public Publisher()
        {
            _producer = InitializeProducerWithRetry();
            Console.WriteLine("[*] Publisher is ready to send messages.");
        }

        private IProducer<string, string> InitializeProducerWithRetry()
        {
            int retryCount = 0;
            int backoffTime = 2000;

            while (retryCount < _maxRetries)
            {
                try
                {
                    var config = new ProducerConfig 
                    { 
                        BootstrapServers = _BootstrapServers,
                        EnableIdempotence = true,
                        Acks = Acks.All,
                        MessageSendMaxRetries = _maxRetries
                    };

                    var producer = new ProducerBuilder<string, string>(config)
                        .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                        .Build();

                    Console.WriteLine("[*] Publisher is ready to send messages."); 
                    return producer;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Failed to connect to Kafka (Attempt {retryCount + 1}/{_maxRetries}): {ex.Message}");
                    if (retryCount == _maxRetries - 1)
                    {
                        Console.WriteLine("[x] Maximum retry attempts reached. Exiting...");
                        throw;
                    }

                    retryCount++;
                    Console.WriteLine($"[*] Retrying in {backoffTime / 1000} seconds.");
                    Thread.Sleep(backoffTime);
                    backoffTime *= 2;
                }
            }
            
            throw new Exception("Unexpected failure in Kafka producer initialization.");
        }
        
        public async Task PublishMessage(Order message)
        {
            string orderJson = JsonConvert.SerializeObject(message);

            try
            {
                var result = await _producer.ProduceAsync(_topicName, new Message<string, string> 
                {
                    Key = message.OrderId,
                    Value = orderJson
                });

                Console.WriteLine($"[x] Sent order ID: {message.OrderId}, items quantity: {message.Items.Count}");
                Console.WriteLine($"Delivered message to {result.Topic}");
            }
            catch (ProduceException<string, string> e)
            {
                Console.WriteLine($"Delivery failed: {e.Error.Reason}");
            }
        }
        
        public async Task PublishUpdateMessage(string orderId, string status)
        {
            var updateMessage = new 
            {
                OrderId = orderId,
                Status = status
            };

            string orderJson = JsonConvert.SerializeObject(updateMessage);

            try
            {
                var result = await _producer.ProduceAsync(_topicName, new Message<string, string> 
                {
                    Key = orderId,
                    Value = orderJson
                });

                Console.WriteLine($"[x] Sent update: OrderId = {orderId}, Status={status}");
                Console.WriteLine($"Delivered message to {result.Topic}");
            }
            catch (ProduceException<string, string> e)
            {
                Console.WriteLine($"Delivery failed: {e.Error.Reason}");
            }
        }
    }
}
