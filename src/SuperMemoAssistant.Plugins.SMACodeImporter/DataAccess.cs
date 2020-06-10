using Anotar.Serilog;
using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.SMACodeImporter
{
  public class DataAccess
  {
    public OrmLiteConnectionFactory dbFactory;
    private string database { get; set; }
    public DataAccess(string database)
    {
      this.database = database;
      this.dbFactory = new OrmLiteConnectionFactory(database, SqliteDialect.Provider);
    }

    public async Task<Extract> GetLatestExtractAsync()
    {
      if (!File.Exists(database))
      {
        LogTo.Warning("Attempted to GetExtractsAsync but DBPath does not exist");
        return null;
      }

      Extract extract = null;
      
      try
      {
        using (var db = dbFactory.Open())
        {
          var extracts = await GetOutstandingExtractsAsync();
          if (extracts != null && extracts.Count > 0)
            extract = extracts.OrderBy(x => x.timestamp).Last();
        }
      }
      catch (Exception e)
      {
        LogTo.Error($"Failed to GetExtracts Async with exception {e}");
      }

      return extract;
    }

    public async Task<List<Extract>> GetOutstandingExtractsAsync()
    {

      if (!File.Exists(database))
      {
        LogTo.Warning("Attempted to GetExtractsAsync but DBPath does not exist");
        return null;
      }

      var extracts = new List<Extract>();

      try
      {
        using (var db = dbFactory.Open())
        {
          extracts = await db.SelectAsync<Extract>(x => !x.exported);
        }
      }
      catch (Exception e)
      {
        LogTo.Error($"Failed to GetExtracts Async with exception {e}");
      }

      return extracts;
    }

    public async Task SetExportedAsync(long Id)
    {
      if (!File.Exists(database))
        LogTo.Warning("Attempted to GetExtractsAsync but DBPath does not exist");

      try
      {
        using (var db = dbFactory.Open())
        {
          await db.UpdateOnlyAsync(() => new Extract { exported = true }, where: x => x.Id == Id);
        }
      }
      catch (Exception e)
      {
        LogTo.Error($"Failed to SetExportedAsync with exception {e}");
      }
    }
  }
}
