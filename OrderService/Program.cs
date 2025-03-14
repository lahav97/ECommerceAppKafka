using OrderService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Add Swagger services to generate API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the OrderConsumer
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
Task.Run(() => consumer.StartConsuming());

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.UseEndpoints(endpoints => { endpoints.MapControllers();});

app.Run();
