using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderService.Models;

namespace OrderService
{
    public class OrderConsumer
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName = "orderQueue";
        private static readonly Dictionary<string, Order> OrdersDatabase = new();

        public OrderConsumer(IConnection connection)
        {
            _connection = connection;
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBind(queue: _queueName, exchange: "orderExchange", routingKey: "new");
        }

        public void StartConsuming()
        {
            try
            {
                var consumer = new EventingBasicConsumer(_channel);
                _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

                consumer.Received += (model, ea) =>
                {
                    var message = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());
                    var order = JsonConvert.DeserializeObject<Order>(message);

                    try
                    {
                        if (order == null || string.IsNullOrEmpty(order.OrderId))
                        {
                            Console.WriteLine("[!] Invalid order received. Discarding the message.");
                            _channel.BasicReject(deliveryTag: ea.DeliveryTag, requeue: false);
                            return;
                        }

                        if (!OrdersDatabase.ContainsKey(order.OrderId))
                        {
                            Console.WriteLine($"[x] Received OrderId: {order.OrderId}, Status: {order.Status}");
                            ProcessOrder(order);
                            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                        }
                        else
                        {
                            Console.WriteLine($"Order {order.OrderId} is already processed");
                            _channel.BasicReject(deliveryTag: ea.DeliveryTag, requeue: false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[!] Error processing order: {ex.Message}");
                        _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                };

                Console.WriteLine("[*] Waiting for messages. To exit press CTRL+C");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Error initializing consumer: {ex.Message}");
            }
        }

        private void ProcessOrder(Order order)
        {
            decimal shippingCost = order.TotalAmount * 0.02m;
            order.TotalAmount += shippingCost;
            order.Status = "processed";
            OrdersDatabase[order.OrderId] = order;

            Console.WriteLine("Order Processed:");
            Console.WriteLine($"OrderId: {order.OrderId}, CustomerId: {order.CustomerId}");
            Console.WriteLine($"Status: {order.Status}, TotalAmount: {order.TotalAmount}, ShippingCost: {shippingCost}"); 
        }

        public Order? GetOrderDetails(string orderId)
        {
            return OrdersDatabase.TryGetValue(orderId, out var order) ? order : null;
        }
    }
}
