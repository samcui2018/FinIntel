
using FinancialIntelligence.Api.Adapters;
using FinancialIntelligence.Api.Repositories;
using FinancialIntelligence.Api.Services;
using System.Text;
using FinancialIntelligence.Api.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using FinancialIntelligence.Api.Services.Insights;
using FinancialIntelligence.Api.Services.Intelligence;
using FinancialIntelligence.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt settings are missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<OpenAiSettings>(
    builder.Configuration.GetSection("OpenAi"));

builder.Services.AddHttpClient<IGenerativeAiClient, OpenAiClient>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))
    .ConfigureHttpClient(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(15);
    });

//services
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBenchmarkService, BenchmarkService>();
builder.Services.AddScoped<IBusinessService, BusinessService>();
builder.Services.AddScoped<ICsvSourceAdapter, CsvSourceAdapter>();
builder.Services.AddScoped<IExcelSourceAdapter, ExcelSourceAdapter>();
builder.Services.AddScoped<IExecutiveSummaryService, RuleBasedExecutiveSummaryService>();
builder.Services.AddScoped<IExecutiveSummaryService, AiExecutiveSummaryService>();
builder.Services.AddScoped<IInsightAnalyzer, InterchangeOptimizationService>();
builder.Services.AddScoped<IInsightRanker, InsightRanker>();
builder.Services.AddScoped<IInsightService, InsightService>();
builder.Services.AddScoped<IInsightAnalyzer, SpendAnomalyInsightService>();
builder.Services.AddScoped<IIntelligenceService, IntelligenceService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITransactionUploadService, TransactionUploadService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<RuleBasedExecutiveSummaryService>();

// Insight generators
builder.Services.AddScoped<IInsightEngine, InsightEngine>();
builder.Services.AddScoped<IInsightGenerator, ConcentrationRiskInsightGenerator>();
builder.Services.AddScoped<IInsightGenerator, DuplicateChargeInsightGenerator>();
builder.Services.AddScoped<IInsightGenerator, TransactionVolumeAnomalyInsightGenerator>();
builder.Services.AddScoped<IInsightGenerator, RecurringSpendInsightGenerator>();
builder.Services.AddScoped<IInsightGenerator, InterchangeOptimizationInsightGenerator>();
builder.Services.AddScoped<IInsightGenerator, SubscriptionWasteInsightGenerator>();
builder.Services.AddScoped<IInsightGenerator, CashFlowForecastInsightGenerator>();
builder.Services.AddScoped<IInsightGenerator, BenchmarkInsightGenerator>();
builder.Services.AddScoped<IInsightGenerator, PredictionInsightGenerator>();

//repositories
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IBusinessRepository, BusinessRepository>();
builder.Services.AddScoped<IBusinessAccessRepository, BusinessAccessRepository>();
builder.Services.AddScoped<IInsightRepository, InsightRepository>();
builder.Services.AddScoped<IInterchangeOptimizationRepository, InterchangeOptimizationRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionQueryRepository, TransactionQueryRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

//builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<IBusinessAuthorizationService, BusinessAuthorizationService>();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddHttpClient<IGenerativeAiClient, OpenAiClient>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// // Enable Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("ReactPolicy");
//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();