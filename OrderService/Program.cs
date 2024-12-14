using OrderService;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
// Add Swagger services to generate API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the RabbitMQ connection as a singleton
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory()
    {
        HostName = builder.Configuration["RABBITMQ_HOST"] ?? "rabbitmq",
        UserName = builder.Configuration["RABBITMQ_USER"] ?? "guest",
        Password = builder.Configuration["RABBITMQ_PASS"] ?? "guest",
        Port = 5672
    };

    return factory.CreateConnection();
});

// Register the OrderConsumer and inject the RabbitMQ connection
builder.Services.AddSingleton<OrderConsumer>();

var app = builder.Build();

// Enable Swagger middleware to generate Swagger as a JSON endpoint
app.UseSwagger();

// Enable Swagger UI middleware to serve the Swagger UI page
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderService API V1");
    c.RoutePrefix = "swagger"; // Available at /swagger
});

// Start OrderConsumer
var consumer = app.Services.GetRequiredService<OrderConsumer>();

// Start consumer in a background thread
Task.Factory.StartNew(() =>
{
    consumer.StartConsuming();
}, TaskCreationOptions.LongRunning);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run();
