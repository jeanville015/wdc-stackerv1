
using System.Text.Json;
using WDC_STACKER.API.Models;

namespace WDC_STACKER.API.Services
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class CapacityConfigService
    {
        private readonly string _contentRootPath;
        private const string DefaultClientKey = "WDC_STACKER.CLIENT";

        private static readonly IReadOnlyDictionary<string, string> ConfigFileNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["WDC_STACKER.CLIENT.PWD"] = "CapacityConfig.WDC_STACKER.CLIENT.PWD.json",
                ["WDC_STACKER.CLIENT.FGI"] = "CapacityConfig.WDC_STACKER.CLIENT.FGI.json"
            };

        public CapacityConfigService(IWebHostEnvironment env)
        {
            _contentRootPath = env.ContentRootPath;
        }

        private string ResolveFilePath(string? clientKey)
        {
            var resolvedClientKey = string.IsNullOrWhiteSpace(clientKey)
                ? DefaultClientKey
                : clientKey.Trim();

            if (!ConfigFileNames.TryGetValue(resolvedClientKey, out var fileName))
                throw new InvalidOperationException($"Unsupported stacker client: {resolvedClientKey}");

            return Path.Combine(_contentRootPath, fileName);
        } 

        public async Task<CapacityConfig> GetAsync(string? clientKey)
        {
            var json = await File.ReadAllTextAsync(ResolveFilePath(clientKey));
            return JsonSerializer.Deserialize<CapacityConfig>(json)!;
        }

        public async Task SaveAsync(CapacityConfig config, string? clientKey)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(ResolveFilePath(clientKey), json);
        }

        public async Task ResetAsync(string? clientKey)
        {
            var isFgi = string.Equals(
                clientKey?.Trim(),
                "WDC_STACKER.CLIENT.FGI",
                StringComparison.OrdinalIgnoreCase);

            var defaults = isFgi
                ? new CapacityConfig
                {
                    RACK_COUNT = 650,
                    LAYER_COUNT = 5,
                    BOX_COUNT = 4,
                    MAX_ITEM_PER_BOX = 10,

                    LAYER_COUNT_SHIPBOX = 5,
                    BOX_COUNT_SHIPBOX = 4,
                    MAX_ITEM_PER_BOX_SHIPBOX = 10,

                    ValidOperation = "302500 BRC OPERATION HOLD",
                    FJ = 3,
                    FD = 3,
                    FS = 3,
                    SJ = "M",
                    SD = "M"
                }
                : new CapacityConfig
                {
                    RACK_COUNT = 3,
                    LAYER_COUNT = 4,
                    BOX_COUNT = 10,
                    MAX_ITEM_PER_BOX = 10,
                    TARGET_QTY = 7200,
                    TARGET_TRAY_COUNT = 30
                };

            await SaveAsync(defaults, clientKey);
        }
    }
}
