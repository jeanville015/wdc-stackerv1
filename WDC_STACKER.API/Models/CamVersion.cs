namespace WDC_STACKER.API.Models
{
    /// <summary>
    /// Canonical cam-version identifiers used throughout the API.
    /// SQL (HOLDER_ASSIGN.CAMVERSION) stores the literal values "3.4"/"7".
    /// appsettings config keys (SoapApi:FeatsEndpoints:*) use "CAM3"/"CAM7"
    /// since "." is not a clean config key segment. This class is the single
    /// place that maps between the two.
    /// </summary>
    public static class CamVersion
    {
        public const string Cam3_4 = "3.4";
        public const string Cam7 = "7";

        public static readonly IReadOnlyList<string> All = new[] { Cam3_4, Cam7 };

        /// <summary>Maps a SQL/business CamVersion value ("3.4"/"7") to its appsettings config key ("CAM3"/"CAM7").</summary>
        public static string ToConfigKey(string camVersion)
        {
            return camVersion.Trim() switch
            {
                Cam3_4 => "CAM3",
                Cam7 => "CAM7",
                _ => throw new InvalidOperationException($"Unknown CamVersion '{camVersion}'.")
            };
        }

        public static bool IsValid(string? camVersion)
        {
            return !string.IsNullOrWhiteSpace(camVersion) &&
                   All.Contains(camVersion.Trim(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
