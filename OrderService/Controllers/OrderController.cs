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

            var order = _orderService.GetOrderDetails(orderId);

            if (order == null)
            {
                return NotFound(new { message = $"Order with order-ID '{orderId}' not found." });
            }

            var shippingCost = CalculateShippingCost(order.TotalAmount);

            return Ok(new
            {
                order.OrderId,
                order.CustomerId,
                order.Status,
                order.TotalAmount,
                ShippingCost = shippingCost
            });
        }

        private decimal CalculateShippingCost(decimal totalAmount)
        {
            return totalAmount * 0.02m;
        }
    }
}
