using Elastic.Apm.NetCoreAll;
using Elastic.Apm.StackExchange.Redis;
using Microsoft.AspNetCore.OutputCaching;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAllElasticApm();

var connectionString = builder.Configuration.GetConnectionString("Redis");
ArgumentNullException.ThrowIfNull(connectionString);
var connection = await ConnectionMultiplexer.ConnectAsync(connectionString).ConfigureAwait(false);
//var connection = ConnectionMultiplexer.Connect(connectionString);
builder.Services.AddSingleton(sp => connection);
connection.UseElasticApm();
//for (int i = 0; i < 10; i++)
//{
//    var database = connection.GetDatabase();
//    await database.StringSetAsync($"redis:string{i}", i).ConfigureAwait(false);
//    await database.StringGetAsync($"redis:string{i}").ConfigureAwait(false);
//}

var app = builder.Build();
// using with Elastic.Apm 1.22.0
//app.UseAllElasticApm(builder.Configuration);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
