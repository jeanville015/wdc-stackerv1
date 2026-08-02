using System.ServiceModel;
using FeatsServiceReference;
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

        private TxnServiceSoapClient CreateClient(string username, string password)
        {
            var binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

            var url = _config["SoapApi:FeatsBaseUrl"];
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("Missing required configuration value: SoapApi:FeatsBaseUrl.");
            }

            var endpoint = new EndpointAddress($"{url.TrimEnd('/')}/FEATS/TxnService.asmx");

            var client = new TxnServiceSoapClient(binding, endpoint);
            client.ClientCredentials.UserName.UserName = username;
            client.ClientCredentials.UserName.Password = password;

            return client;
        }

        public async Task<UserPrivilegesResponse> GetUserPrivilegesAsync(string employeeName, string username, string password)
        {
            _logger.LogInformation(
                "FEATS GetUserPrivileges → employee={Employee}", employeeName);

            try
            {
                using var client = CreateClient(username, password);
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

        public async Task<FeatsQueryResponse> QueryAsync(FeatsQueryRequest request, string username,string password)
        {
            _logger.LogInformation(
                "FEATS Query -> queryType={QueryType}, recordLimit={RecordLimit}",
                request.QueryType,
                request.RecordLimit);

            // Add domain with <username>
            string usernameWDomain = "AD/" + username;

            try
            {
                using var client = CreateClient(usernameWDomain, password);

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
        public async Task<(bool Success, string Message)> MoveOutAsync(string holder, string? holderType, string? resource, string? nextOp, string username, string password)
        {
            _logger.LogInformation("FEATS MoveOut -> holder={Holder}",holder);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password);
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
        public async Task<(bool Success, string Message)> MoveInAsync(string holder, string? holderType, string? resource, string username, string password)
        {
            _logger.LogInformation("FEATS MoveIn -> holder={Holder}", holder);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password);
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
        public async Task<(bool Success, string Message)> AddJobAsync(string holder, string holderType, FeatsServiceReference.child_holder_info[] newHolders, bool allowMixingJobAttributes, string username, string password)
        {
            _logger.LogInformation("FEATS AddJob -> holder={Holder}, holderType={HolderType}, newHolderCount={NewHolderCount}", holder, holderType, newHolders.Length);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password);
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
        public async Task<(bool Success, string Message)> HoldHolderAsync(string holder, string? holderType, string holdReasonCode, string comment, string username, string password)
        {
            _logger.LogInformation("FEATS HoldHolder -> holder={Holder}, holdReasonCode={HoldReasonCode}", holder, holdReasonCode);

            var usernameWithDomain = username.StartsWith(
                "AD/",
                StringComparison.OrdinalIgnoreCase)
                    ? username
                    : $"AD/{username}";

            try
            {
                using var client = CreateClient(usernameWithDomain, password);
                await client.HoldHolderAsync(holder, holderType!, holdReasonCode, comment);
                return (true, "FEATS HoldHolder completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FEATS HoldHolder failed for holder={Holder}", holder);
                return (false, $"FEATS HoldHolder failed: {ex.Message}");
            }
        }

    }
}
