using Newtonsoft.Json;
using OrderService.Models;
using Confluent.Kafka;

namespace OrderService
{
    public class OrderConsumer
    {
        private readonly string _topicName = "ordersTopic";
        private readonly string _GroupId = "orderConsumer";
        private readonly string _BootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BROKER") ?? "kafka:9092";
         private readonly ConsumerConfig _config;
        private static readonly Dictionary<string, Order> OrdersDatabase = new();
        private static readonly Dictionary<string, List<Order>> TopicOrderDatabase = new();
        IConsumer<string, string>? _OrderToConsume;

        public OrderConsumer()
        {
            _config = new ConsumerConfig
            {
                BootstrapServers = _BootstrapServers,
                GroupId = _GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };
        }

        public void StartConsuming()
        {
            try
            {
                _OrderToConsume = new ConsumerBuilder<string, string>(_config)
                .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                .Build();

                _OrderToConsume.Subscribe(_topicName);
                
                Console.WriteLine($"[*] Subscribed to topic: {_topicName} for new orders and updates");
                Console.WriteLine("[*] Waiting for messages. To exit press CTRL+C");

                ProcessMessages();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Consumer shutting down...");
            }   
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error initializing consumer: {ex.Message}");
            }
            finally
            {
                _OrderToConsume.Close();
            }    
        }

        private void ProcessMessages()
        {
            while (true)
            {                                  
                try
                {
                    var result = _OrderToConsume.Consume(TimeSpan.FromMilliseconds(100));

                    if (result != null && result.Message.Value != null)
                    {
                        string jsonMessage = result.Message.Value;

                        if (jsonMessage.Contains("\"Items\"") && jsonMessage.Contains("\"TotalAmount\""))
                        {
                            var newOrder = JsonConvert.DeserializeObject<Order>(jsonMessage);

                            if (newOrder != null && newOrder.Items != null)
                            {
                                ProcessOrder(newOrder);
                            }
                        }
                        else if (jsonMessage.Contains("\"OrderId\"") && jsonMessage.Contains("\"Status\""))
                        {
                            var orderUpdate = JsonConvert.DeserializeObject<OrderUpdate>(jsonMessage);

                            if (orderUpdate != null && !string.IsNullOrEmpty(orderUpdate.OrderId))
                            {
                                ProcessOrderUpdate(orderUpdate);
                            }
                        }
                        else
                        {
                            Console.WriteLine("[!] Unrecognized message format. Discarding.");
                        }

                        _OrderToConsume.Commit(result);
                    }
                }
                catch (JsonSerializationException ex)
                {
                    Console.WriteLine($"[!] JSON deserialization error: {ex.Message}");
                }
                catch (KafkaException ke)
                {
                    Console.WriteLine($"[!] Kafka error while consuming: {ke.Error.Reason}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Error processing message: {ex.Message}");
                }
            }    
        }

        private bool ProcessOrder(Order order)
        {
            bool res = false;

            if(ValidateOrderAction(order, "new"))
            {
                HandleNewOrder(order);
                AddOrderToTopicDict(_topicName, order);
                res = true;
            }
            
            return res;
        }

        private void AddOrderToTopicDict(string topicName, Order order)
        {
            if(!TopicOrderDatabase.ContainsKey(topicName))
            {
                TopicOrderDatabase[topicName] = new List<Order>();
            }

            TopicOrderDatabase[topicName].Add(order);
        }
        private bool ValidateOrderAction(Order order, string actionType)
        {
            if (string.IsNullOrEmpty(order.OrderId))
            {
                Console.WriteLine("[!] Validation error: Order ID is null or empty.");
                return false;
            }
            else if (!int.TryParse(order.OrderId, out int intOrderId) || intOrderId <= 0)
            {
                Console.WriteLine($"[!] Validation failed: Order ID '{order.OrderId}' is not a valid integer.");
                return false;
            }
            else if (actionType == "update" && string.IsNullOrEmpty(order.Status))
            {
                Console.WriteLine($"[!] Validation failed: Status is null or empty for Order ID '{order.OrderId}'.");
                return false;
            }
            else if(OrdersDatabase.ContainsKey(order.OrderId))
            {
                Console.WriteLine($"Order {order.OrderId} is already processed");
                return false;  
            }
            return true;
        }

        private void HandleNewOrder(Order order)
        {
            if (!OrdersDatabase.ContainsKey(order.OrderId) && order.Status == "new")
            {
                Console.WriteLine($"[x] Received OrderId: {order.OrderId}, Status: {order.Status}");
                ProcessAndStoreOrder(order);
            }
            else
            {
                Console.WriteLine($"Order {order.OrderId} is already processed");
            }
        }

        private void ProcessAndStoreOrder(Order order)
        {
            decimal shippingCost = CalculateShippingCost(order.TotalAmount);
            order.TotalAmount += shippingCost;
            order.Status = "processed";
            OrdersDatabase[order.OrderId] = order;

            Console.WriteLine($"Order ID: {order.OrderId}, Customer ID: {order.CustomerId}");
            Console.WriteLine($"Status: {order.Status}, Total Amount: {order.TotalAmount}, Shipping Cost: {shippingCost}"); 
        }

        public void UpdateExistingOrder(string orderId, string status)
        {
            if (OrdersDatabase.TryGetValue(orderId, out var existingOrder))
            {
                if(existingOrder.Status == status)
                {
                    Console.WriteLine($"Order {orderId} is already in status {status}");
                    return;
                }
                else if(status == "deleted" || status == "cancelled")
                {
                    OrdersDatabase.Remove(orderId);
                    TopicOrderDatabase[_topicName].Remove(existingOrder);
                    Console.WriteLine($"[x] OrderId: {orderId} has been deleted.");
                    return;
                }
                else
                {
                    existingOrder.Status = status;
                    OrdersDatabase[orderId] = existingOrder;
                    Console.WriteLine($"[x] Received update for OrderId: {orderId}, from Status: {existingOrder.Status} to Status: {status}");
                }
            }
            else
            {
                Console.WriteLine($"Order {orderId} not found");
            }
        }

        private void ProcessOrderUpdate(OrderUpdate update)
        {
            if (string.IsNullOrEmpty(update.OrderId) || string.IsNullOrEmpty(update.Status))
            {
                Console.WriteLine("[!] Invalid update message. Missing OrderId or Status.");
                return;
            }

            UpdateExistingOrder(update.OrderId, update.Status);
        }
 
        private static decimal CalculateShippingCost(decimal totalAmount)
        {
            return totalAmount * 0.02m;
        }
        
        public Order? GetOrderDetails(string orderId)
        {
            return OrdersDatabase.TryGetValue(orderId, out var order) ? order : null;
        }

        public List<string>? GetAllOrderIdsFromTopic(string topicName)
        {
            if(!TopicOrderDatabase.ContainsKey(topicName))
            {
                Console.WriteLine($"[!] Topic '{topicName}' not found.");
                return new List<string>();
            }
            
            var  orderIds = TopicOrderDatabase[topicName]
                .Where(order => !string.IsNullOrEmpty(order.OrderId))
                .Select(order => order.OrderId)
                .ToList();

            return orderIds;
        }
    }
}
