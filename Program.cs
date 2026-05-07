//class Program
//{

//    //static void Main(string[] args)
//    //{
//    //    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JGaF5cXGpCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWXxfd3RcQmZeUEJ2VkBWYEs=");
//    //    var service = new PdfToImageServiceNew();
//    //    string inputRoot = @"\\fs1\OFFICE";
//    //   // "HTTPS://certificates.gemapp.be/PDFtoImage/1301/12000_aankoopAspeco_Page_1.png"
//    //    string outputRoot = @"\\fs1\CertificateData\ALL\PDFtoImage";

//    //    service.ProcessAllDatabases(inputRoot, outputRoot);

//    //    Console.WriteLine(" Conversion Completed");

//    //}
//}
using FrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SSRSPdfToImageService.Service.IOParams;
using System.Runtime.CompilerServices;
using InputParameters = SSRSPdfToImageService.Service.IOParams.InputParameters;
using OutputParameters = SSRSPdfToImageService.Service.IOParams.OutputParameters;

namespace SSRSPdfToImageService
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var services = ConfigureService(args);
                var serviceProvider = services.BuildServiceProvider(); //create Service Providers

                var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Starting application");

                //var contextChecker = serviceProvider.GetService<IContextChecker>();
                //contextChecker!.SetConsoleContext();

                await Start(serviceProvider, logger, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task Start(IServiceProvider serviceProvider, ILogger<Program> logger, string[] args)
        {
            TraceHelperService.StartMethod(logger, args);

            Service.IOParams.InputOutputParameters<InputParameters, OutputParameters>? _InputOutputParameters;

            try
            {
                _InputOutputParameters = serviceProvider.GetRequiredService<Service.IOParams.InputOutputParameters<InputParameters, OutputParameters>>();
                await _InputOutputParameters.Run(null);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(string.Format("{0}{1}{2}", ex.Message, Environment.NewLine, ex.StackTrace));
            }

            TraceHelperService.EndMethod(logger);
        }

        private static IServiceCollection ConfigureService(string[] args)
        {


            IServiceCollection tServices = new ServiceCollection();
            var Configuration = LoadConfiguration(args);

            tServices.AddSingleton(Configuration);
            Log.Logger = new LoggerConfiguration()
              .ReadFrom.Configuration(Configuration) // Read settings from appsettings.json
              .CreateLogger();

            tServices.AddSingleton(Configuration);

            tServices.AddLogging((loggingBuilder) =>
            {
                //loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);

                //// Skip EF Core SQL queries in file logs
                loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                loggingBuilder.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);

            });

            tServices.AddInputOutputParameters();
            tServices.AddSingleton<PdfToImageServiceNew>();

            //tServices.AddDbContext<IDiaAdminContext, DiaAdminDbContext>(options =>
            //    options.UseSqlServer(CommandLineArguments.Parameter("DbDal.ConnectionString"))
            //           .ConfigureWarnings(w =>
            //               w.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS)));
            //tServices.AddDbContext<IDiaAdminContext, DiaAdminDbContext>(options =>
            //    options.UseSqlServer(
            //        CommandLineArguments.Parameter("DbDal.ConnectionString"),
            //        sql =>
            //        {
            //            sql.CommandTimeout(60 * 60 * 24);
            //        })
            //    .ConfigureWarnings(w =>
            //        w.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS))
            //);


            // tServices.AddValueService();

            return tServices;
        }
        public static class GlobalLogger
        {
            private static readonly Serilog.ILogger _logger = Log.ForContext(typeof(GlobalLogger));

            public static void LogInformation(string message, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string methodName = "")
            {
                string fileName = Path.GetFileName(callerFilePath);
                _logger.Information("[{FileName}] [{MethodName}] {Message}", fileName, methodName, message);
            }

            public static void LogError(string message, Exception ex, [CallerFilePath] string callerFilePath = "", [CallerMemberName] string methodName = "")
            {
                string fileName = Path.GetFileName(callerFilePath);
                _logger.Error(ex, "[{FileName}] [{MethodName}] {Message}", fileName, methodName, message);
            }
        }
        private static IConfiguration LoadConfiguration(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "PRD";

            return new ConfigurationBuilder()
               .SetBasePath(Directory.GetParent(AppContext.BaseDirectory)!.FullName)
               .AddJsonFile("appsettings.json", true, true)
               .AddJsonFile($"appsettings.{env}.json", true, true)
               .AddEnvironmentVariables()
               .AddUserSecrets<Program>(true)
               .AddCommandLine(args)
               .Build();
        }
    }
}