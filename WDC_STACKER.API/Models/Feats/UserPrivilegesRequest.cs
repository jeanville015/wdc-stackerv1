namespace WDC_STACKER.API.Models.Feats
{
    // ── Inbound (from React TS client → your Web API) ─────────────────────────
    public class UserPrivilegesRequest
    {
        public string EmployeeName { get; set; } = string.Empty;
        public string FeatsUsername { get; set; } = string.Empty;
        public string FeatsPassword { get; set; } = string.Empty;
    }

    // ── Outbound (your Web API → React TS client) ─────────────────────────────
    // The FEATS service returns raw XML in <GetUserPrivilegesResult>.
    // We expose both the parsed-out fields (once you know the XML schema)
    // and the raw XML as a fallback / debugging aid.
    public class UserPrivilegesResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;

        /// <summary>
        /// Raw XML returned by FEATS — useful until the inner schema is confirmed.
        /// Replace with strongly-typed privilege fields once the XML shape is known.
        /// </summary>
        public string RawPrivilegesXml { get; set; } = string.Empty;

        // ── TODO: replace / extend with real privilege fields ──────────────────
        // Example fields — delete or rename once real schema is known:
        // public List<string> Roles      { get; set; } = new();
        // public bool         CanApprove { get; set; }
        // public bool         CanAudit   { get; set; }

        public XmlTableResult? ParsedPrivileges { get; set; }
    }

    public class XmlTableResult
    {
        public string RootName { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public List<Dictionary<string, string>> Rows { get; set; } = new();
    }

}
