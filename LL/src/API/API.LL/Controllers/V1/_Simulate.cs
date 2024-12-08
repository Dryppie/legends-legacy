using Common.Utilities;
using Domain.Models.Abilities;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace API.LL.Controllers.V1;

public class _Simulate : BaseController
{
    [HttpGet("SimulateCombat")]
    public async Task<IActionResult> SimulateCombat()
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "abilities.json");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound($"JSON file not found at path: {filePath}");
        }

        string json = await System.IO.File.ReadAllTextAsync(filePath);

        // Deserialize JSON into a list of abilities
        List<Ability> abilities = JsonSerializer.Deserialize<List<Ability>>(json, AbilityJsonReader.Options)!;

        return Ok(abilities);
    }
}
