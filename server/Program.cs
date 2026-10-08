var builder = WebApplication.CreateBuilder(args);

// Allow CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        builder => builder.WithOrigins("http://172.22.6.98:610"
        , "http://localhost:3000"
        ,"http://localhost:610"
        ,"http://g-app1:610") 
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});

// ADD THESE TWO LINES — this was missing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();
var configuration = builder.Configuration;
var connectionString = configuration.GetConnectionString("ECWDB");

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(
    c=>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "server v1");
        c.RoutePrefix=string.Empty;
    });

app.UseCors("AllowReact");

app.UseAuthorization();
app.MapControllers();
app.Run();
