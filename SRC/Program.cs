var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

string[] secrets = 
{
    "mysql_constr"
};

SecretService secretService = new SecretService(secrets);
var connectionString = secretService["mysql_constr"] ?? throw new Exception("Failed to fetch connection string from Docker Secret");

DataService dataService = new DataService(connectionString);


builder.Services.AddSingleton<SecretService>(secretService);
builder.Services.AddSingleton<DataService>(dataService);

var app = builder.Build();

app.MapControllers();

app.Run();
