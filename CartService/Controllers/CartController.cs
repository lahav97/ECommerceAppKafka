using Microsoft.AspNetCore.Mvc;
using CartService.Models;

namespace CartService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CartController : ControllerBase
    {
        private readonly Publisher _publisher;

        public CartController(Publisher publisher)
        {
            _publisher = publisher;
        }

        [HttpPost("create-order")]
        public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (request.ItemsNum < 0)
            {
                return BadRequest(new { message = "Invalid number of items. Must be positive number of items !" });
            }
            else if(request.OrderId[0] == '-')
            {
                return BadRequest(new { message = "OrderId cannot be negative." });
            }

            var orderBuilder = new OrderBuilder(request.OrderId, request.ItemsNum);
            var order = orderBuilder.Build();
            _publisher.PublishMessage(order); 

            return Ok(new { message = "Order created successfully!", orderId = order.OrderId });
        }
        
        [HttpPost("update-order")]
        public IActionResult UpdateOrder([FromBody] CreateOrderUpdate request)
        {
            if (string.IsNullOrEmpty(request.OrderId))
            {
                return BadRequest(new { message = "OrderId cannot be null or empty." });
            }
            else if(request.OrderId[0] == '-')
            {
                return BadRequest(new { message = "OrderId cannot be negative." });
            }

            _publisher.PublishUpdateMessage(request.OrderId, request.Status); 

            return Ok(new { message = "Order updated sent to the consumer!", orderId = request.OrderId });
        }
    }
}
