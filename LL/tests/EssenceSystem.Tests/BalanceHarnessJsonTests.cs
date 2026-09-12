using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BalanceHarness;

namespace EssenceSystem.Tests;

[Trait("Category", "BalanceHarness")]
public sealed class BalanceHarnessJsonTests
{
    [Theory]
    [InlineData("utf8")]
    [InlineData("utf8-bom")]
    [InlineData("utf16")]
    public void Buffered_archive_reads_preserve_encoding_and_exact_file_hashes(string encodingName)
    {
        var encoding = encodingName switch
        {
            "utf8-bom" => new UTF8Encoding(true),
            "utf16" => Encoding.Unicode,
            _ => new UTF8Encoding(false)
        };
        var value = string.Concat(Enumerable.Repeat("Essence æ水 🌲", 40000));
        var json = JsonSerializer.Serialize(new Dictionary<string, string> { ["value"] = value });
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(json)).ToArray();
        var path = Path.Combine(Path.GetTempPath(), "tower-json-" + Guid.NewGuid().ToString("N"));
        try
        {
            File.WriteAllBytes(path, bytes);
            Assert.Equal(value, HarnessJson.Read<Dictionary<string, string>>(path)["value"]);
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), HarnessJson.FileHash(path));
            bytes[bytes.Length / 2] ^= 1;
            File.WriteAllBytes(path, bytes);
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(bytes)), HarnessJson.FileHash(path));
        }
        finally { File.Delete(path); }
    }
}
