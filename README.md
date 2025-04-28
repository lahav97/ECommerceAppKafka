# ECommerceAppRabbit

**Full Name:** Lahav Rabinovitz  

## Topics Used - `ordersTopic` and the Producer and the Consumer used it.
### Producer
  - **Purpose:** This topic is used by the `CartService` to publish new orders and order updates.
                 The `Publisher` class in the `CartService` sends messages to this topic, which are then consumed by the `OrderService`.

### Consumer
  - **Purpose:** This topic is used by the `OrderService` to consume new orders and order updates.
                 The `OrderConsumer` class in the `OrderService` listens to this topic and processes the messages accordingly.

## Message Key
- **Key:** `OrderId`
  - **Reason:** The `OrderId` is used as the key in the message to ensure that each message can be uniquely identified and processed.
                This helps in maintaining the integrity of the order data and allows for efficient retrieval and updates of specific orders.
                also this helps to guarantee that all messages for a given order are processed sequentially by the consumer,
                which is crucial for maintaining consistency in processing order events (e.g., ensuring that updates to an order are processed in the correct order).
## Error Handling
### Producer
  - In the producer, I used a retry mechanism with exponential backoff when trying to establish a connection to Kafka. If the connection fails,
    the producer will retry 5 times, with each attempt waiting a progressively longer time (e.g., 2, 4, 8 seconds).
    This approach ensures that the producer doesn’t fail immediately on the first failure and gives the Kafka server time to recover in case of temporary issues.

### Consumer
  - Kafka Consumer Connection Error Handling:
    Similar to the producer, the consumer also implements a retry mechanism with exponential backoff for connecting to Kafka. 
    The consumer retries up to 5 times in case of a failure, allowing the system to recover from temporary Kafka downtimes.
  

Input Validation:
	In the producer's /create-order and /update-order routes, input validation checks are in place to ensure that the required fields (orderId and itemsNum in create order, and orderId and status in update order) are provided and valid. 
    If not, a 400 Bad Request response with an appropriate error message is returned.
    This ensures that invalid requests are rejected early to prevent unnecessary processing.
Exception Handling:
	Both the producer and consumer routes are wrapped in try-except blocks to catch and handle any unexpected exceptions. 
    If any exception occurs, the error message is captured, and a 500 Internal Server Error response is returned with a description of the issue.
    This ensures that unexpected errors are handled gracefully, and the user receives feedback on what went wrong.
