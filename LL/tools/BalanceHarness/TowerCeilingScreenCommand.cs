using System.Text.Json;

namespace BalanceHarness;

public static class TowerCeilingScreenCommand
{
    public static async Task<int> ExecuteAsync(string[] args, CancellationToken token = default)
    {
        object result;
        if (args.Length == 4 && args[0] == "tower-ceiling-audit")
        {
            if (Path.Exists(args[3])) throw new IOException("Audit output must be new.");
            result = await TowerCeilingScreenInputs.AuditMaterializationAsync(args[1], args[2], token);
            HarnessJson.WriteNew(args[3], result);
        }
        else if (args.Length == 3 && args[0] == "tower-ceiling-bind")
            result = await TowerCeilingScreenInputs.BindAsync(args[1], args[2], token);
        else if (args.Length == 2 && args[0] == "tower-ceiling-check")
            result = TowerCeilingScreenInputs.VerifyPrepared(args[1], token);
        else if (args.Length == 2 && args[0] == "tower-ceiling-run")
            result = await TowerCeilingScreenRun.RunAsync(args[1], token, Console.WriteLine);
        else if (args.Length == 2 && args[0] == "tower-ceiling-verify")
            result = await TowerCeilingScreenRun.VerifyAsync(args[1], token);
        else throw new InvalidDataException("Use tower-ceiling-audit <prepared> <manifest-hash> <new-result.json>, tower-ceiling-bind <binding-request.json> <new-study>, or tower-ceiling-check|tower-ceiling-run|tower-ceiling-verify <study>. No cap overrides, retries, resumes or seed allocator.");
        // Detailed protocol/measurements remain in the archive; keep terminal output bounded.
        Console.WriteLine(JsonSerializer.Serialize(new { command = args[0], status = "Complete", resultType = result.GetType().Name }, HarnessJson.Options));
        return 0;
    }
}
