using System.Collections.Generic;

namespace FinancialIntelligence.Api.Models
{
    public sealed class PythonInsightsOptions
    {
        public string PythonExe { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 60;
        public Dictionary<string, string> Scripts { get; set; } = new();
    }
}