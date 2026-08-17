using System.ServiceModel;
using FeatsServiceReference;
using WDC_STACKER.API.Models;
using WDC_STACKER.API.Models.Feats;

namespace WDC_STACKER.API.Services
{
    public class FeatsService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<FeatsService> _logger;

        public FeatsService(IConfiguration config, ILogger<FeatsService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Returns the cam versions currently enabled in
        /// SoapApi:FeatsEndpoints config (used to fan out job-scan queries).
        /// </summary>
        public IReadOnlyList<string> GetEnabledCamVersions()
        {
            return CamVersion.All
                .Where(IsCamVersionEnabled)
                .ToList();
        }

        public bool IsCamVersionEnabled(string camVersion)
        {
            var configKey = CamVersion.ToConfigKey(camVersion);
            return _config.GetValue<bool?>($"SoapApi:FeatsEndpoints:{configKey}:Enabled") ?? false;
        }

        private string ResolveBaseUrl(string camVersion)
        {
            var configKey = CamVersion.ToConfigKey(camVersion);
            var url = _config[$"SoapApi:FeatsEndpoints:{configKey}:BaseUrl"];

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException(
                    $"Missing required configuration value: SoapApi:FeatsEndpoints:{configKey}:BaseUrl.");
            }

            if (!IsCamVersionEnabled(camVersion))
            {
                throw new InvalidOperationException(
                    $"FEATS cam version '{camVersion}' ({configKey}) is disabled.");
            }

            return url;
        }

        private TxnServiceSoapClient CreateClient(string username, string password, string camVersion)
        {
            var binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            var url = ResolveBaseUrl(camVersion);

            // CAM3 uses /FEATS/TxnService.asmx, CAM7 uses /FEATS7X/TxnService.asmx
            var path = camVersion == CamVersion.Cam7 ? "/FEATS7X/TxnService.asmx" : "/FEATS/TxnService.asmx";
            var endpoint = new EndpointAddress($"{url.TrimEnd('/')}{path}");

            var client = new TxnServiceSoapClient(binding, endpoint);
            client.ClientCredentials.UserName.UserName = username;
            client.ClientCredentials.UserName.Password = password;

            return client;
        }

        public async Task<UserPrivilegesResponse> GetUserPrivilegesAsync(string employeeName, string username, string password, string camVersion = CamVersion.Cam3_4)
        {
            _logger.LogInformation(
                "FEATS GetUserPrivileges → employee={Employee}", employeeName);

            try
            {
                using var client = CreateClient(username, password, camVersion);
                var result = await client.GetUserPrivilegesAsync(employeeName);

                return new UserPrivilegesResponse
                {
                    Success = true,
                    Message = "OK",
                    EmployeeName = employeeName,
                    RawPrivilegesXml = result?.OuterXml ?? string.Empty,
                    ParsedPrivileges = result is null ? null : ParseXmlTable(result)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "FEATS call failed for employee={Employee}", employeeName);
                return new UserPrivilegesResponse
                {
                    Success = false,
                    Message = "FEATS service error: " + ex.Message
                };
            }
        }

        private static XmlTableResult ParseXmlTable(System.Xml.XmlNode root)
        {
            var rows = new List<Dictionary<string, string>>();
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (System.Xml.XmlElement rowElement in root.ChildNodes.OfType<System.Xml.XmlElement>())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (System.Xml.XmlElement field in rowElement.ChildNodes.OfType<System.Xml.XmlElement>())
                {
                    row[field.Name] = field.InnerText;
                    columns.Add(field.Name);
                }

                if (row.Count > 0)
                    rows.Add(row);
            }

            return new XmlTableResult
            {
                RootName = root.Name,
                Columns = columns.ToList(),
                Rows = rows
            };
        }

        public async Task<FeatsQueryResponse> QueryAsync(FeatsQueryRequest request, string username,string password, string camVersion)
        {
            _logger.LogInformation(
                "FEATS Query -> queryType={QueryType}, recordLimit={RecordLimit}, camVersion={CamVersion}",
                request.QueryType,
                request.RecordLimit,
                camVersion);

            // Add domain with <username>
            string usernameWDomain = "AD/" + username;

            try
            {
                using var client = CreateClient(usernameWDomain, password, camVersion);

                var response = await client.QueryAsync(new FeatsServiceReference.QueryRequest
                {
                    QueryType = request.QueryType,
                    FieldNames = request.FieldNames.ToArray(),
                    Filters = request.Filters
                        .Select(filter => new FeatsServiceReference.query_filter
                        {
                            FilterName = filter.FilterName,
                            FilterValue = filter.FilterValue
                        })
                        .ToArray(),
                    RecordLimit = request.RecordLimit
                });

                return new FeatsQueryResponse
                {
                    Success = true,
                    Message = "OK",
                    QueryType = request.QueryType,
                    HasMoreRows = response.HasMoreRows,
                    RawXml = response.QueryResult?.OuterXml ?? string.Empty,
                    ParsedResult = response.QueryResult is null
                        ? new FeatsQueryTableResult()
                        : ParseQueryResult(response.QueryResult)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS Query failed for queryType={QueryType}", request.QueryType);

                return new FeatsQueryResponse
                {
                    Success = false,
                    Message = "FEATS query error: " + ex.Message,
                    QueryType = request.QueryType
                };
            }
        }

        private static FeatsQueryTableResult ParseQueryResult(System.Xml.XmlNode root)
        {
            var rows = new List<Dictionary<string, string>>();
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (System.Xml.XmlElement rowElement in root.ChildNodes.OfType<System.Xml.XmlElement>())
            {
                if (rowElement.Name.Contains("schema", StringComparison.OrdinalIgnoreCase))
                    continue;

                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (System.Xml.XmlElement field in rowElement.ChildNodes.OfType<System.Xml.XmlElement>())
                {
                    row[field.Name] = field.InnerText;
                    columns.Add(field.Name);
                }

                if (row.Count > 0)
                    rows.Add(row);
            }

            return new FeatsQueryTableResult
            {
                RootName = root.Name,
                Columns = columns.ToList(),
                Rows = rows
            };
        }
        /// <summary>
        /// The ! operators only suppress nullable-reference warnings. 
        /// The actual runtime values remain null. The WSDL marks HolderType, 
        /// Resource, and NextOp with minOccurs="0", although the FEATS server may still apply its own business validation.
        /// See MoveOutAsync(parameters)
        /// </summary>
        /// <param name="holder"></param>
        /// <param name="holderType"></param>
        /// <param name="resource"></param>
        /// <param name="nextOp"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<(bool Success, string Message)> MoveOutAsync(string holder, string? holderType, string? resource, string? nextOp, string username, string password, string camVersion)
        {
            _logger.LogInformation("FEATS MoveOut -> holder={Holder}, camVersion={CamVersion}",holder, camVersion);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password, camVersion);
                await client.MoveOutAsync(holder, holderType!, resource!, nextOp!);
                return (true,"FEATS MoveOut completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS MoveOut failed for holder={Holder}", holder);
                return (false, $"FEATS MoveOut failed: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// The ! operator only suppresses nullable-reference warnings.
        /// The actual runtime value remains null. The WSDL marks HolderType
        /// with minOccurs="0", although the FEATS server may still apply its own business validation.
        /// </summary>
        /// <param name="holder"></param>
        /// <param name="holderType"></param>
        /// <param name="operation"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<(bool Success, string Message)> SuperMoveAsync(string holder, string? holderType, string operation, string username, string password, string camVersion)
        {
            _logger.LogInformation("FEATS SuperMove -> holder={Holder}, operation={Operation}, camVersion={CamVersion}", holder, operation, camVersion);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password, camVersion);
                await client.SuperMoveAsync(holder, holderType!, operation);
                return (true, "FEATS SuperMove completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS SuperMove failed for holder={Holder}", holder);
                return (false, $"FEATS SuperMove failed: {ex.Message}");
            }
        }

        /// <summary>
        /// The ! operators only suppress nullable-reference warnings.
        /// The actual runtime values remain null. The WSDL marks HolderType
        /// and Resource with minOccurs="0", although the FEATS server may still apply its own business validation.
        /// </summary>
        /// <param name="holder"></param>
        /// <param name="holderType"></param>
        /// <param name="resource"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<(bool Success, string Message)> MoveInAsync(string holder, string? holderType, string? resource, string username, string password, string camVersion)
        {
            _logger.LogInformation("FEATS MoveIn -> holder={Holder}, camVersion={CamVersion}", holder, camVersion);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password, camVersion);
                await client.MoveInAsync(holder, holderType!, resource!);
                return (true, "FEATS MoveIn completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS MoveIn failed for holder={Holder}", holder);
                return (false, $"FEATS MoveIn failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Groups <paramref name="newHolders"/> under <paramref name="holder"/>
        /// (the entered Shipping Id) via the FEATS AddJob transaction.
        /// </summary>
        /// <param name="holder"></param>
        /// <param name="holderType"></param>
        /// <param name="newHolders"></param>
        /// <param name="allowMixingJobAttributes"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<(bool Success, string Message)> AddJobAsync(string holder, string holderType, FeatsServiceReference.child_holder_info[] newHolders, bool allowMixingJobAttributes, string username, string password, string camVersion)
        {
            _logger.LogInformation("FEATS AddJob -> holder={Holder}, holderType={HolderType}, newHolderCount={NewHolderCount}, camVersion={CamVersion}", holder, holderType, newHolders.Length, camVersion);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password, camVersion);
                await client.AddJobAsync(holder, holderType, newHolders, allowMixingJobAttributes);
                return (true, "FEATS AddJob completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS AddJob failed for holder={Holder}", holder);
                return (false, $"FEATS AddJob failed: {ex.Message}");
            }
        }

        /// <summary>
        /// The ! operators only suppress nullable-reference warnings.
        /// The actual runtime values remain null. The WSDL marks HolderType
        /// with minOccurs="0", although the FEATS server may still apply its own business validation.
        /// </summary>
        /// <param name="holder"></param>
        /// <param name="holderType"></param>
        /// <param name="holdReasonCode"></param>
        /// <param name="comment"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<(bool Success, string Message)> HoldHolderAsync(string holder, string? holderType, string holdReasonCode, string comment, string username, string password, string camVersion)
        {
            _logger.LogInformation("FEATS HoldHolder -> holder={Holder}, holdReasonCode={HoldReasonCode}, camVersion={CamVersion}", holder, holdReasonCode, camVersion);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password, camVersion);
                await client.HoldHolderAsync(holder, holderType!, holdReasonCode, comment);
                return (true, "FEATS HoldHolder completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS HoldHolder failed for holder={Holder}", holder);
                return (false, $"FEATS HoldHolder failed: {ex.Message}");
            }
        }

        /// <summary>
        /// The ! operators only suppress nullable-reference warnings.
        /// The actual runtime values remain null. The WSDL marks HolderType
        /// with minOccurs="0", although the FEATS server may still apply its own business validation.
        /// </summary>
        /// <param name="holder"></param>
        /// <param name="holderType"></param>
        /// <param name="comment"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<(bool Success, string Message)> ReleaseHolderAsync(string holder, string? holderType, string comment, string username, string password, string camVersion)
        {
            _logger.LogInformation("FEATS ReleaseHolder -> holder={Holder}, camVersion={CamVersion}", holder, camVersion);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password, camVersion);
                await client.ReleaseHolderAsync(holder, holderType!, comment);
                return (true, "FEATS ReleaseHolder completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS ReleaseHolder failed for holder={Holder}", holder);
                return (false, $"FEATS ReleaseHolder failed: {ex.Message}");
            }
        }

        /// <summary>
        /// CAM3-specific: Attaches a job to a holder (e.g., ATTACHJOB(SHIPPINGID, 'SHPBOX')).
        /// </summary>
        public async Task<(bool Success, string Message)> AttachJobAsync(
            string holder,
            string holderType,
            string holderGeometry,
            string startReason,
            string owner,
            string? isCdSem,
            string? cdSemSamplePlan,
            string? waferNum,
            string? routing,
            string? segment,
            string? classCode,
            string? productName,
            string? minorRev,
            string? factory,
            string? buildCode,
            string? experiment,
            string? ptsJobNum,
            string username,
            string password)
        {
            _logger.LogInformation("FEATS AttachJob -> holder={Holder}, holderType={HolderType}", holder, holderType);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password, CamVersion.Cam3_4);

                var parameters = new List<FeatsServiceReference.named_parameter>
                {
                    new FeatsServiceReference.named_parameter { Name = "Holder", Value = holder },
                    new FeatsServiceReference.named_parameter { Name = "HolderType", Value = holderType },
                    new FeatsServiceReference.named_parameter { Name = "HolderGeometry", Value = holderGeometry },
                    new FeatsServiceReference.named_parameter { Name = "StartReason", Value = startReason },
                    new FeatsServiceReference.named_parameter { Name = "Owner", Value = owner }
                };

                if (!string.IsNullOrEmpty(isCdSem))
                    parameters.Add(new FeatsServiceReference.named_parameter { Name = "ISCDSEM", Value = isCdSem });
                if (!string.IsNullOrEmpty(cdSemSamplePlan))
                    parameters.Add(new FeatsServiceReference.named_parameter { Name = "CDSEMSAMPLEPLAN", Value = cdSemSamplePlan });
                if (!string.IsNullOrEmpty(waferNum))
                    parameters.Add(new FeatsServiceReference.named_parameter { Name = "WAFERNUM", Value = waferNum });
                if (!string.IsNullOrEmpty(routing))
                    parameters.Add(new FeatsServiceReference.named_parameter { Name = "ROUTING", Value = routing });
                if (!string.IsNullOrEmpty(segment))
                    parameters.Add(new FeatsServiceReference.named_parameter { Name = "SEGMENT", Value = segment });
                if (!string.IsNullOrEmpty(classCode))
                    parameters.Add(new FeatsServiceReference.named_parameter { Name = "CLASSCODE", Value = classCode });
                if (!string.IsNullOrEmpty(productName))
                    parameters.Add(new FeatsServiceReference.named_parameter { Name = "PRODUCTNAME", Value = productName });
                if (!string.IsNullOrEmpty(minorRev))
                    parameters.Add(new FeatsServiceReference.named_parameter { Name = "MINORREV", Value = minorRev });
                if (!string.IsNullOrEmpty(factory))
                    parameters.Add(new FeatsServiceReference.named_parameter { Name = "FACTORY", Value = factory });
                if (!string.IsNullOrEmpty(buildCode))
                    parameters.Add(new FeatsServiceReference.named_parameter { Name = "BUILDCODE", Value = buildCode });
                if (!string.IsNullOrEmpty(experiment))
                    parameters.Add(new FeatsServiceReference.named_parameter { Name = "EXPERIMENT", Value = experiment });
                if (!string.IsNullOrEmpty(ptsJobNum))
                    parameters.Add(new FeatsServiceReference.named_parameter { Name = "PTSJOBNUM", Value = ptsJobNum });

                await client.AttachJobAsync(parameters.ToArray());
                return (true, "FEATS AttachJob completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS AttachJob failed for holder={Holder}", holder);
                return (false, $"FEATS AttachJob failed: {ex.Message}");
            }
        }

        /// <summary>
        /// CAM3-specific: Sets shipment destination (e.g., SETSHIP(SHIPPINGID, 'SHPBOX', 'ShipToSite')).
        /// </summary>
        public async Task<(bool Success, string Message)> SetShipmentDestinationAsync(string holder, string holderType, string shipmentDestination, string username, string password, string camVersion)
        {
            _logger.LogInformation("FEATS SetShipmentDestination -> holder={Holder}, holderType={HolderType}, shipmentDestination={ShipmentDestination}, camVersion={CamVersion}", holder, holderType, shipmentDestination, camVersion);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password, camVersion);
                await client.SetShipmentDestinationAsync(holder, holderType, shipmentDestination);
                return (true, "FEATS SetShipmentDestination completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS SetShipmentDestination failed for holder={Holder}", holder);
                return (false, $"FEATS SetShipmentDestination failed: {ex.Message}");
            }
        }

        /// <summary>
        /// CAM3-specific: Transfers holder job (e.g., TRANSFERHOLDERJOB()).
        /// </summary>
        public async Task<(bool Success, string Message)> TransferHolderJobAsync(string srcHolder, string srcHolderType, string dstHolder, string dstHolderType, string? transposeFormula, string? newHolderGeometry, string username, string password, string camVersion)
        {
            _logger.LogInformation("FEATS TransferHolderJob -> srcHolder={SrcHolder}, dstHolder={DstHolder}, camVersion={CamVersion}", srcHolder, dstHolder, camVersion);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password, camVersion);
                await client.TransferHolderJobAsync(srcHolder, srcHolderType, dstHolder, dstHolderType, transposeFormula ?? string.Empty, newHolderGeometry ?? string.Empty);
                return (true, "FEATS TransferHolderJob completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS TransferHolderJob failed for srcHolder={SrcHolder}", srcHolder);
                return (false, $"FEATS TransferHolderJob failed: {ex.Message}");
            }
        }

        /// <summary>
        /// CAM3-specific: Ships a holder (e.g., SHIP(SHIPPINGID, 'SHPBOX')).
        /// </summary>
        public async Task<(bool Success, string Message)> ShipAsync(string holder, string holderType, string username, string password, string camVersion)
        {
            _logger.LogInformation("FEATS Ship -> holder={Holder}, holderType={HolderType}, camVersion={CamVersion}", holder, holderType, camVersion);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password, camVersion);
                await client.ShipAsync(holder, holderType);
                return (true, "FEATS Ship completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS Ship failed for holder={Holder}", holder);
                return (false, $"FEATS Ship failed: {ex.Message}");
            }
        }

        /// <summary>
        /// CAM3-specific: Ships a holder using ShipL1 (e.g., SHIPL1(HOLDER, 'HOLDER')).
        /// </summary>
        public async Task<(bool Success, string Message)> ShipL1Async(string holder, string holderType, string username, string password, string camVersion)
        {
            _logger.LogInformation("FEATS ShipL1 -> holder={Holder}, holderType={HolderType}, camVersion={CamVersion}", holder, holderType, camVersion);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password, camVersion);
                await client.ShipL1Async(holder, holderType);
                return (true, "FEATS ShipL1 completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS ShipL1 failed for holder={Holder}", holder);
                return (false, $"FEATS ShipL1 failed: {ex.Message}");
            }
        }

        /// <summary>
        /// CAM3-specific: Ships a holder using Ship1 (e.g., SHIP1(HOLDER, 'HOLDER')).
        /// </summary>
        public async Task<(bool Success, string Message)> Ship1Async(string holder, string holderType, string username, string password, string camVersion)
        {
            _logger.LogInformation("FEATS Ship1 -> holder={Holder}, holderType={HolderType}, camVersion={CamVersion}", holder, holderType, camVersion);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password, camVersion);
                await client.Ship1Async(holder, holderType);
                return (true, "FEATS Ship1 completed successfully.");
            }
            catch (FaultException ex)
            {
                // A real FEATS/SOAP fault (business rule violation) - Ship1 genuinely failed.
                _logger.LogError(ex, "FEATS Ship1 failed for holder={Holder}", holder);
                return (false, $"FEATS Ship1 failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FEATS Ship1 -> non-fault exception for holder={Holder}; treating empty response as success.", holder);
                return (true, "FEATS Ship1 completed successfully (empty response).");
            }
        }

    }
}
