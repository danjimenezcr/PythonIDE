using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PyStudioDesktopSharp.Services;

public sealed class SignatureService
{
    private const string LocalKey = "PYSTUDIO_SIGNATURE_KEY_2026";

    public string SignFile(string scriptPath, string signaturePath)
    {
        string content = File.ReadAllText(scriptPath);
        string signature = CreateSignature(content);
        var signatures = Load(signaturePath);
        signatures[Path.GetFileName(scriptPath)] = signature;
        Save(signaturePath, signatures);
        return signature;
    }

    public bool VerifyFile(string scriptPath, string signaturePath)
    {
        var signatures = Load(signaturePath);
        string fileName = Path.GetFileName(scriptPath);
        if (!signatures.TryGetValue(fileName, out string? savedSignature))
            return false;

        string current = CreateSignature(File.ReadAllText(scriptPath));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(current),
            Encoding.UTF8.GetBytes(savedSignature));
    }

    private static string CreateSignature(string content)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(LocalKey));
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static Dictionary<string, string> Load(string signaturePath)
    {
        if (!File.Exists(signaturePath))
            return new Dictionary<string, string>();

        try
        {
            string json = File.ReadAllText(signaturePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static void Save(string signaturePath, Dictionary<string, string> signatures)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(signaturePath)!);
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(signaturePath, JsonSerializer.Serialize(signatures, options));
    }
}
