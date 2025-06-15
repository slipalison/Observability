using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Mvc;

namespace Observability.WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly Counter<int> _forecastCounter; // NOSSO NOVO CONTADOR

    // Injetamos o Meter para criar o contador
    public WeatherForecastController(ILogger<WeatherForecastController> logger, Meter meter)
    {
        _logger = logger;
        // Criamos o contador com um nome, unidade e descrição
        _forecastCounter = meter.CreateCounter<int>(
            name: "app.weather.forecast.count",
            unit: "{forecasts}",
            description: "Counts the number of weather forecasts generated, tagged by summary.");
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public async Task<ActionResult<IEnumerable<WeatherForecast>>> Get()
    {
        var forecasts = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        
        // INCREMENTAMOS O CONTADOR PARA CADA PREVISÃO GERADA
        foreach (var forecast in forecasts)
        {
            // Adicionamos 1 ao contador, junto com um label (Tag) para o tipo de summary
            _forecastCounter.Add(1, new KeyValuePair<string, object?>("summary_type", forecast.Summary));
        }

        _logger.LogInformation("Generated {ForecastCount} weather forecasts.", forecasts.Length);
        return Ok(forecasts);
    }
}


