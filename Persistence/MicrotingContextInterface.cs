using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HistorySql
{
    interface MicrotingContextInterface : IDisposable
    {
        DbSet<check_lists> Check_lists { get; set; }
        DbSet<fields> Fields { get; set; }
        DbSet<log_exceptions> Log_exceptions { get; set; }
        DbSet<logs> Logs { get; set; }
        DbSet<settings> Settings { get; set; }

        int SaveChanges();

        Database Database { get; }
    }
}