using System.ServiceModel;
using Microsoft.Extensions.Logging;
using AhsServiceReference;

namespace WDC_STACKER.API.Services
{
    public class AhsService
    {
        private readonly ILogger<AhsService> _logger;
        private readonly string _ahsServiceUrl;

        public AhsService(ILogger<AhsService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _ahsServiceUrl = configuration["AHS:ServiceUrl"] ?? "http://pbt-mt-ahsapp01:1010/AutoHolding.asmx";
        }

        private AutoHoldingSoapClient CreateClient()
        {
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None);
            var endpoint = new EndpointAddress(_ahsServiceUrl);
            return new AutoHoldingSoapClient(binding, endpoint);
        }

        /// <summary>
        /// Calls AHS SliderCheck2 to check if holder has slider issues
        /// </summary>
        public async Task<(bool Success, string Message, string RawResponse)> SliderCheck2Async(string holder, string operation, bool checkExist)
        {
            _logger.LogInformation("AHS SliderCheck2 -> holder={Holder}, operation={Operation}", holder, operation);

            try
            {
                using var client = CreateClient();
                var result = await client.SliderCheck2Async(holder, operation, checkExist);
                
                return (true, result, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AHS SliderCheck2 failed for holder={Holder}", holder);
                return (false, $"AHS SliderCheck2 failed: {ex.Message}", string.Empty);
            }
        }

        /// <summary>
        /// Calls AHS CheckHold to check if holder is on hold
        /// </summary>
        public async Task<(bool Success, string Message, bool IsOnHold)> CheckHoldAsync(string holder, string currentOp)
        {
            _logger.LogInformation("AHS CheckHold -> holder={Holder}, currentOp={CurrentOp}", holder, currentOp);

            try
            {
                using var client = CreateClient();
                var result = await client.CheckHoldAsync(holder, currentOp);
                
                // Parse the result - assuming the service returns a string indicating hold status
                var isOnHold = !string.IsNullOrWhiteSpace(result) && 
                    (result.Equals("HOLD", StringComparison.OrdinalIgnoreCase) ||
                     result.Contains("HOLD", StringComparison.OrdinalIgnoreCase));

                return (true, result, isOnHold);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AHS CheckHold failed for holder={Holder}", holder);
                return (false, $"AHS CheckHold failed: {ex.Message}", false);
            }
        }
    }
}
