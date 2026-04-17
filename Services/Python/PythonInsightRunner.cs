using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FinancialIntelligence.Api.Configuration;
using FinancialIntelligence.Api.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialIntelligence.Api.Services
{
    public sealed class PythonInsightRunner : IPythonInsightRunner
    {
        private readonly PythonInsightsOptions _options;
        private readonly ILogger<PythonInsightRunner> _logger;
        private readonly IConfiguration _configuration;

        public PythonInsightRunner(
            IOptions<PythonInsightsOptions> options,
            ILogger<PythonInsightRunner> logger,
            IConfiguration configuration)
        {
            _options = options.Value;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<PythonInsightProcessResult> RunAsync(
            string scriptKey,
            string businessId,
            string asOfDate,
            int monthsBack,
            CancellationToken cancellationToken = default)
        {
            var scriptPath = ResolveScriptPath(scriptKey);
            ValidateConfiguration(scriptPath);

            var arguments =
                $"\"{scriptPath}\" " +
                $"--connectionString \"{BuildConnectionString()}\" " +
                $"--businessId \"{businessId}\" " +
                $"--asOfDate \"{asOfDate}\" " +
                $"--monthsBack {monthsBack}";

            var startInfo = new ProcessStartInfo
            {
                FileName = _options.PythonExe,
                Arguments = arguments,
                WorkingDirectory = _options.WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = startInfo };

            _logger.LogInformation(
                "Starting Python insight script. ScriptKey={ScriptKey}, ScriptPath={ScriptPath}, BusinessId={BusinessId}, AsOfDate={AsOfDate}, MonthsBack={MonthsBack}",
                scriptKey, scriptPath, businessId, asOfDate, monthsBack);

            process.Start();

            var stdOutTask = process.StandardOutput.ReadToEndAsync();
            var stdErrTask = process.StandardError.ReadToEndAsync();

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // best effort
                }

                if (timeoutCts.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        $"Python insight script timed out after {_options.TimeoutSeconds} seconds. ScriptKey={scriptKey}");
                }

                throw;
            }

            var stdout = await stdOutTask;
            var stderr = await stdErrTask;

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                _logger.LogWarning(
                    "Python insight script wrote to stderr. ScriptKey={ScriptKey}, Stderr={Stderr}",
                    scriptKey, stderr);
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Python insight script failed. ScriptKey={scriptKey}, ExitCode={process.ExitCode}, StdErr={stderr}");
            }

            if (string.IsNullOrWhiteSpace(stdout))
            {
                throw new InvalidOperationException(
                    $"Python insight script returned empty stdout. ScriptKey={scriptKey}");
            }

            PythonInsightProcessResult? result;
            try
            {
                result = JsonSerializer.Deserialize<PythonInsightProcessResult>(
                    stdout,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to deserialize Python insight output. ScriptKey={ScriptKey}, StdOut={StdOut}",
                    scriptKey, stdout);

                throw new InvalidOperationException(
                    $"Failed to deserialize Python insight output for script '{scriptKey}'.", ex);
            }

            if (result is null)
            {
                throw new InvalidOperationException(
                    $"Python insight script returned null result. ScriptKey={scriptKey}");
            }

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                throw new InvalidOperationException(
                    $"Python insight script returned application error. ScriptKey={scriptKey}, Error={result.Error}");
            }

            return result;
        }

        private string ResolveScriptPath(string scriptKey)
        {
            if (!_options.Scripts.TryGetValue(scriptKey, out var configuredPath) ||
                string.IsNullOrWhiteSpace(configuredPath))
            {
                throw new InvalidOperationException(
                    $"Missing PythonInsights:Scripts:{scriptKey} configuration.");
            }

            return Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(_options.WorkingDirectory, configuredPath);
        }

        private void ValidateConfiguration(string scriptPath)
        {
            if (string.IsNullOrWhiteSpace(_options.PythonExe))
                throw new InvalidOperationException("PythonInsights:PythonExe is missing.");

            if (string.IsNullOrWhiteSpace(_options.WorkingDirectory))
                throw new InvalidOperationException("PythonInsights:WorkingDirectory is missing.");

            if (!File.Exists(_options.PythonExe))
                throw new FileNotFoundException("Configured Python executable was not found.", _options.PythonExe);

            if (!Directory.Exists(_options.WorkingDirectory))
                throw new DirectoryNotFoundException(
                    $"Configured Python working directory was not found: {_options.WorkingDirectory}");

            if (!File.Exists(scriptPath))
                throw new FileNotFoundException("Configured Python script was not found.", scriptPath);
        }

        private string BuildConnectionString()
        {
            // Replace this with your actual source of the FinIntel connection string.
            // Example if you already inject IConfiguration or a settings object:
            return _configuration.GetConnectionString("OdbcConnectionString")
               ?? throw new InvalidOperationException("Missing Odbc connection string.");

            throw new NotImplementedException(
                "Wire BuildConnectionString() to your Odbc connection string source.");
        }
    }
}