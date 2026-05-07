using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using XSS.Data.Generic.IServices;
using XSS.Models.Generic.Interface;
using XSS.Models.Generic.Model;
using XSS.Models.Generic.Model.Diagnostics;

namespace SSRSPdfToImangeService.DbContexts.AppDbContext
{
        [Keyless]
    public class StringResult 
    {
        public string type { get; set; }
    }

    /// <summary>
    /// Use to interacting with the database.
    /// </summary>
    public class DiaAdminDbContext : DbContext, IDiaAdminContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiaAdminDbContext"/> class.
        /// </summary>
        /// <param name="pOptions">The options to be used by a DbContext.</param>
        /// <param name="pITraceApiService">The trace API service.</param>
        public DiaAdminDbContext(DbContextOptions<DiaAdminDbContext> pOptions, ITraceApiService pITraceApiService)
            :base (pOptions)
        { }

        public DbSet<StringResult> Result { get; set; }
        public DbSet<IDAdminModel> IDAdmin { get; set; }

        public DbSet<EmailLogsModel> EmailLogs { get; set; }
        public DbSet<MailRemarksModel> MailRemarks { get; set; }

        public DbSet<OptionsGlobalModel> OptionsGlobal { get; set; }

        public DbSet<RequestLogModel> RequestLog { get; set; }

        /// <summary>
        ///    Configure DbContext
        /// </summary>
        /// <param name="pOptionsBuilder">Provides a simple API for configuring an IMutableEntityType</param>
        protected override void OnConfiguring(DbContextOptionsBuilder pOptionsBuilder)
        {
            pOptionsBuilder.EnableSensitiveDataLogging();
            base.OnConfiguring(pOptionsBuilder);
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder pConfigurationBuilder)
        {
            pConfigurationBuilder.DefaultTypeMapping<decimal>(x => x.HasPrecision(28, 15));
            pConfigurationBuilder.Properties<decimal>().HavePrecision(28, 15);
            pConfigurationBuilder.Properties<decimal?>().HavePrecision(28, 15);

            pConfigurationBuilder.Properties<DateTime>().HaveConversion(typeof(DateTimeUtcConverter));
            pConfigurationBuilder.Properties<DateTime?>().HaveConversion(typeof(DateTimeNullAbleUtcConverter));
        }

        public void SetManualConnectionStringValues(string pToken)
        {
            throw new NotImplementedException();
        }

        public class DateTimeUtcConverter : ValueConverter<DateTime, DateTime>
        {
            public DateTimeUtcConverter() : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
            {
            }
        }
        public class DateTimeNullAbleUtcConverter : ValueConverter<DateTime?, DateTime?>
        {
            public DateTimeNullAbleUtcConverter() : base(v => v.HasValue ? v.Value : v, v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
            {
            }
        }
    }
}
