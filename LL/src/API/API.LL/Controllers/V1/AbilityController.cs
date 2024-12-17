using Common.Utilities;
using Domain.Models.Essences;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace API.LL.Controllers.V1;

public class AbilityController : BaseController
{
    [HttpGet("readAll")]
    public async Task<IActionResult> ReadAllAbilitiesFromFile()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "abilities.json");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound($"JSON file not found at path: {filePath}");
        }

        string json = await System.IO.File.ReadAllTextAsync(filePath);

        // Deserialize JSON into a list of essences
        List<Essence> essences = JsonSerializer.Deserialize<List<Essence>>(json, AbilityJsonReader.Options)!;

        return Ok(essences);
    }
}
