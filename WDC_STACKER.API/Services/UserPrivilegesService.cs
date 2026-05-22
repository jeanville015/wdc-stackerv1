using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using WDC_STACKER.API.Models;
using WDC_STACKER.API.Models.Feats;

namespace WDC_STACKER.API.Services
{
    /// <summary>
    /// Calls the FEATS TxnService SOAP 1.2 endpoint and returns parsed privileges.
    /// </summary>
    public class UserPrivilegesService
    {
        private readonly HttpClient _http;
        private readonly ILogger<UserPrivilegesService> _logger;

        // appsettings.json  →  "SoapApi:FeatsBaseUrl"
        //   e.g. "http://hchasspda1o.legacy.shared"
        private readonly string _featsBaseUrl;

        private const string SoapPath = "/FEATS/TxnService.asmx";
        private const string FeatNamespace = "http://sjhasspdn1.snjtest1.sanjose.ibm.com/FEATS";

        public UserPrivilegesService(HttpClient http,
                                     IConfiguration config,
                                     ILogger<UserPrivilegesService> logger)
        {
            _http = http;
            _logger = logger;
            _featsBaseUrl = config["SoapApi:FeatsBaseUrl"]
                                ?? "http://hchasspda1o.legacy.shared";
        }

        // ── Public entry point ────────────────────────────────────────────────
        public async Task<UserPrivilegesResponse> GetAsync(UserPrivilegesRequest request)
        {
            _logger.LogInformation(
                "GetUserPrivileges → {BaseUrl}{Path} for employee={Employee}",
                _featsBaseUrl, SoapPath, request.EmployeeName);

            try
            {
                var soapEnvelope = BuildSoapEnvelope(request.EmployeeName);
                var httpRequest = BuildHttpRequest(soapEnvelope);
                var httpResponse = await _http.SendAsync(httpRequest);

                var body = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "FEATS returned {StatusCode}: {Body}",
                        (int)httpResponse.StatusCode, body);

                    return new UserPrivilegesResponse
                    {
                        Success = false,
                        Message = $"FEATS service error: HTTP {(int)httpResponse.StatusCode}"
                    };
                }

                return ParseSoapResponse(body, request.EmployeeName);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error reaching FEATS");
                return new UserPrivilegesResponse
                {
                    Success = false,
                    Message = "Cannot reach FEATS service. Check network / host configuration."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetUserPrivileges");
                return new UserPrivilegesResponse
                {
                    Success = false,
                    Message = "Unexpected error processing FEATS response."
                };
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string BuildSoapEnvelope(string employeeName)
        {
            // Sanitise employee name to prevent XML injection
            var safeName = System.Security.SecurityElement.Escape(employeeName);

            return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <soap12:Envelope
                    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                    xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                    xmlns:soap12="http://www.w3.org/2003/05/soap-envelope">
                  <soap12:Body>
                    <GetUserPrivileges xmlns="{FeatNamespace}">
                      <EmployeeName>{safeName}</EmployeeName>
                    </GetUserPrivileges>
                  </soap12:Body>
                </soap12:Envelope>
                """;
        }

        private HttpRequestMessage BuildHttpRequest(string soapEnvelope)
        {
            var url = $"{_featsBaseUrl.TrimEnd('/')}{SoapPath}";

            var message = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(soapEnvelope, Encoding.UTF8)
            };

            // SOAP 1.2 requires application/soap+xml
            message.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/soap+xml") { CharSet = "utf-8" };

            return message;
        }

        private UserPrivilegesResponse ParseSoapResponse(string soapXml, string employeeName)
        {
            try
            {
                var doc = XDocument.Parse(soapXml);
                XNamespace soap = "http://www.w3.org/2003/05/soap-envelope";
                XNamespace feats = FeatNamespace;

                // Navigate:  Envelope → Body → GetUserPrivilegesResponse → GetUserPrivilegesResult
                var resultElement = doc
                    .Element(soap + "Envelope")?
                    .Element(soap + "Body")?
                    .Element(feats + "GetUserPrivilegesResponse")?
                    .Element(feats + "GetUserPrivilegesResult");

                if (resultElement is null)
                {
                    _logger.LogWarning("GetUserPrivilegesResult element not found in FEATS response");
                    return new UserPrivilegesResponse
                    {
                        Success = false,
                        Message = "Unexpected SOAP response structure from FEATS."
                    };
                }

                var rawXml = resultElement.Value;   // inner XML string returned by FEATS

                // TODO: once the inner XML schema is known, parse rawXml into typed fields.
                // Example:
                //   var inner = XDocument.Parse(rawXml);
                //   var roles = inner.Descendants("Role").Select(r => r.Value).ToList();

                return new UserPrivilegesResponse
                {
                    Success = true,
                    Message = "OK",
                    EmployeeName = employeeName,
                    RawPrivilegesXml = rawXml
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse FEATS SOAP response");
                return new UserPrivilegesResponse
                {
                    Success = false,
                    Message = "Failed to parse FEATS SOAP response."
                };
            }
        }
    }
}
