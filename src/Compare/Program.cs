var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();

app.MapGet("/start-httpClient", async () =>
{
    var count = 0;
    try
    {
        Console.WriteLine("Start");
        for (var i = 0; i < 100000; i++)
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync("http://localhost:5235/weatherforecast");
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine(content);
            count++;
        }

        Console.WriteLine("Done");
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        return Results.BadRequest($"{e.Message} Max: {count}");
    }

    return Results.Ok();
});

app.MapGet("/start-IHttpClientFactory", async (IHttpClientFactory httpClientFactory) =>
{
    try
    {
        Console.WriteLine("Start");
        for (var i = 0; i < 100; i++)
        {
            var httpClient = httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync("http://localhost:5235/weatherforecast");
            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine(content);
            Console.WriteLine(Environment.NewLine);
        }

        Console.WriteLine("Done");
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }

    return Results.Ok();
});

app.Run();
