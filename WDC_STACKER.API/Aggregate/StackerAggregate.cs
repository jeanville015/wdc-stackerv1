using WDC_STACKER.API.Models.Feats;
using WDC_STACKER.API.Models.Stacker;
using WDC_STACKER.API.Services;

namespace WDC_STACKER.API.Aggregate
{
    public class StackerAggregate
    {
        private readonly FeatsService _featsService;
        private readonly FeatsCredentialStore _credentialStore;
        private readonly CapacityConfigService _capacityConfigService;

        public StackerAggregate(FeatsService featsService, FeatsCredentialStore credentialStore, CapacityConfigService capacityConfigService)
        {
            _featsService = featsService;
            _credentialStore = credentialStore;
            _capacityConfigService = capacityConfigService;
        }

        public async Task<ScanHolderJobResponse> ScanHolderJobAsync(string holder, string token)
        {
            if (!_credentialStore.TryGet(token, out var credentials))
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "Invalid or expired token."
                };
            }

            var fieldNames = new[]
            {
        "Holder",
        "Job",
        "JobLevel",
        "Operation",
        "ProductName",
        "BinName",
        "ClassName",
        "ShipTicket",
        "ParentHolder",
        "Routing",
        "Workflow",
        "WaferNum"
    };

            var holderJobResult = await ExecuteFeatsQueryAsync(
                Holder: holder,
                queryType: "HolderJob",
                fieldNames: fieldNames,
                filterName: "Holder",
                filterValue: holder,
                recordLimit: 250,
                username: credentials.Username,
                password: credentials.Password);

            if (!holderJobResult.Success)
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = holderJobResult.Message,
                    RawQueryResult = holderJobResult
                };
            }

            var row = holderJobResult.ParsedResult.Rows.FirstOrDefault();

            if (row is null)
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "HolderJob record was not found.",
                    RawQueryResult = holderJobResult
                };
            }

            var config = await _capacityConfigService.GetAsync();

            var operation = GetField(row, "Operation");
            var parentHolder = GetField(row, "ParentHolder");
            var shipTicket = GetField(row, "ShipTicket");

            // 1. Operation must match config.ValidOperation
            if (!string.Equals(operation, config.ValidOperation, StringComparison.OrdinalIgnoreCase))
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "Operation is not valid",
                    HolderJob = row,
                    RawQueryResult = holderJobResult
                };
            }

            // 2. ParentHolder must have value
            if (string.IsNullOrWhiteSpace(parentHolder))
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "ParentHolder has no value!",
                    HolderJob = row,
                    RawQueryResult = holderJobResult
                };
            }

            // 3. ShipTicket must be empty
            if (!string.IsNullOrWhiteSpace(shipTicket))
            {
                return new ScanHolderJobResponse
                {
                    Success = false,
                    CanAssign = false,
                    Holder = holder,
                    Message = "ShipTicket has value!",
                    HolderJob = row,
                    RawQueryResult = holderJobResult
                };
            }

            return new ScanHolderJobResponse
            {
                Success = true,
                CanAssign = true,
                Holder = holder,
                Message = "Validation Pass!",
                HolderJob = row,
                RawQueryResult = holderJobResult
            };
        }

        private static string GetField(Dictionary<string, string> row, string fieldName)
        {
            return row.TryGetValue(fieldName, out var value)
                ? value?.Trim() ?? string.Empty
                : string.Empty;
        }

        private async Task<FeatsQueryResponse> ExecuteFeatsQueryAsync(string Holder, string queryType, string[] fieldNames, string filterName, string filterValue, int recordLimit, string username, string password)
        {
            var request = new FeatsQueryRequest
            {
                QueryType = queryType,
                FieldNames = fieldNames.ToList(),
                Filters = new List<FeatsQueryFilter>
                {
                    new FeatsQueryFilter
                    {
                        FilterName = filterName,
                        FilterValue = filterValue
                    }
                },
                RecordLimit = recordLimit
            };

            return await _featsService.QueryAsync(request, username, password);
        }
    }
}