# ECommerceAppRabbit - CartService

## Developer Information
**Full Name:** Lahav Rabinovitz  
**ID Number:** 209028349

## API Endpoints
### Producer Application
- **URL:** `http://localhost:5099/api/orders`
- **Type:** POST
- **Description:** This API allows clients to place a new order. The producer sends the order data to a RabbitMQ exchange, which is then consumed by the consumer for further processing.

### Consumer Application
- **URL:** No direct URL for the consumer. It listens for incoming messages from RabbitMQ.
- **Type of Request:** RabbitMQ queue consumer
- **Description:** The consumer does not expose an HTTP API. Instead, it listens for messages on the orderQueue from the RabbitMQ exchange named orderExchange. Once a message is received, the consumer processes the order and updates its status to processed.

## Exchange Type
**Type:** Direct Exchange  
**Reason:** A direct exchange was chosen because it allows messages to be routed to queues based on specific routing keys. This is useful for our application as it enables precise control over which messages are sent to which queues, ensuring that each consumer receives only the messages it is interested in.

## Binding Key
**Binding Key:** `new`  
**Reason:** The binding key `new` is used to ensure that only new orders are routed to the consumer. This helps in filtering the messages and ensures that the consumer processes only relevant messages.

## Exchange and Queue Declaration
**Service:** Producer Service  
**Reason:** The producer service declares the exchange and queue because it is responsible for sending messages to the RabbitMQ server. By declaring the exchange and queue, the producer ensures that the necessary infrastructure is in place before any messages are sent.