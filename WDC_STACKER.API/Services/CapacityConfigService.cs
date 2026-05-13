
using System.Text.Json;
using WDC_STACKER.API.Models;

namespace WDC_STACKER.API.Services
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class CapacityConfigService
    {
        private readonly string _filePath;

        public CapacityConfigService(IWebHostEnvironment env)
        {
            // File sits in the Web API root folder
            _filePath = Path.Combine(env.ContentRootPath, "CapacityConfig.json");
        }

        // READ
        public async Task<CapacityConfig> GetAsync()
        {
            var json = await File.ReadAllTextAsync(_filePath);
            return JsonSerializer.Deserialize<CapacityConfig>(json)!;
        }

        // WRITE (covers Create, Update)
        public async Task SaveAsync(CapacityConfig config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_filePath, json);
        }

        // RESET to defaults
        public async Task ResetAsync()
        {
            var defaults = new CapacityConfig
            {
                RACK_COUNT = 3,
                LAYER_COUNT = 4,
                BOX_COUNT = 10,
                TARGET_QTY = 7200,
                TARGET_TRAY_COUNT = 30
            };
            await SaveAsync(defaults);
        }
    }
}
