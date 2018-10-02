namespace HistorySql
{

    using System.Data.Entity;

    public partial class MicrotingDbMs : DbContext, MicrotingContextInterface
    {
        public MicrotingDbMs() { }

        public MicrotingDbMs(string connectionString)
          : base(connectionString)
        {
        }

        public virtual DbSet<check_lists> Check_lists { get; set; }
        public virtual DbSet<fields> Fields { get; set; }
        public virtual DbSet<log_exceptions> Log_exceptions { get; set; }
        public virtual DbSet<logs> Logs { get; set; }
        public virtual DbSet<settings> Settings { get; set; }
    }
}
