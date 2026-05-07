using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SSRSPdfToImageService.Service.IOParams
{
    public static class InputOutputParameters
    {
        public static void AddInputOutputParameters(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<InputParameters>();
            serviceCollection.AddSingleton<OutputParameters>();
            serviceCollection.AddSingleton<InputOutputParameters<InputParameters, OutputParameters>>();
        }
    }

    public class InputOutputParameters<T, U> : FrameworkCore.InputOutputParameters<T, U>
        where T : InputParameters
        where U : OutputParameters
    {
        private ILogger<InputOutputParameters<T, U>> _logger;

        public InputOutputParameters(IServiceProvider serviceProvider, ILogger<InputOutputParameters<T, U>> plogger) : base(serviceProvider, plogger)
        {
            _logger = plogger;
        }

        protected override async Task Run()
        {
            try
            {
                switch (InputParameters.Start.ToUpper())
                {
                    case "PDFTOIMAGE":
                    case "" /* Default */:
                        {
                            await ServiceProvider.GetRequiredService<PdfToImageServiceNew>().Run(InputParameters, OutputParameters);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        public override void ShowHelp()
        {
        }
    }

    public class InputParameters : FrameworkCore.InputParameters
    {
        public InputParameters() { }

        public string DBName { get; set; }
        public string TransactionType { get; set; } = "Purchases";


    }

    public class OutputParameters : FrameworkCore.OutputParameters
    {
        public object DataResult { get; set; }
    }
}
