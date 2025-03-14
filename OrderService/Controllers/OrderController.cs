using Microsoft.AspNetCore.Mvc;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderConsumer _orderService;

        public OrdersController(OrderConsumer orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("order-details")]
        public IActionResult GetOrderDetails([FromQuery] string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return BadRequest(new { message = "OrderId cannot be null or empty." });
            }
            else if(orderId[0] == '-')
            {
                return BadRequest(new { message = "OrderId cannot be negative." });
            }

            var order = _orderService.GetOrderDetails(orderId);

            if (order == null)
            {
                return NotFound(new { message = $"Order with order-ID '{orderId}' not found." });
            }

            return Ok(new
                {
                    order.OrderId,
                    order.CustomerId,
                    order.Status,
                    order.TotalAmount,
                    Shipping = CalculateShippingCost(order.TotalAmount)
                }
            );
        }

        [HttpPost("getAllOrderIdsFromTopic")]
        public IActionResult GetAllOrderIdsFromTopic([FromBody] string topicName)
        {
            if (string.IsNullOrEmpty(topicName))
            {
                return BadRequest(new { message = "Topic name cannot be null or empty." });
            }

            var orderIds = _orderService.GetAllOrderIdsFromTopic(topicName);
            
            if (orderIds == null || !orderIds.Any())
            {
                return NotFound(new { message = $"No order IDs found for topic: {topicName}" });
            }
            return Ok(new { orderIds });
        }
        private decimal CalculateShippingCost(decimal totalAmount)
        {
            return totalAmount * 0.02m;
        }
    }
}
