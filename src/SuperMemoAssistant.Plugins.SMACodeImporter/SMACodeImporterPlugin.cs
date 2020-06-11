#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// 
// 
// Created On:   6/2/2020 4:20:47 PM
// Modified By:  james

#endregion




namespace SuperMemoAssistant.Plugins.SMACodeImporter
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics.CodeAnalysis;
  using System.IO;
  using System.Linq;
  using System.Threading.Tasks;
  using System.Windows;
  using System.Windows.Input;
  using Anotar.Serilog;
  using Highlight;
  using SuperMemoAssistant.Extensions;
  using SuperMemoAssistant.Interop.SuperMemo.Content.Contents;
  using SuperMemoAssistant.Interop.SuperMemo.Elements.Builders;
  using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
  using SuperMemoAssistant.Plugins.SMACodeImporter.UI;
  using SuperMemoAssistant.Services;
  using SuperMemoAssistant.Services.IO.HotKeys;
  using SuperMemoAssistant.Services.IO.Keyboard;
  using SuperMemoAssistant.Services.Sentry;
  using SuperMemoAssistant.Services.UI.Configuration;
  using SuperMemoAssistant.Sys.IO.Devices;

  // ReSharper disable once UnusedMember.Global
  // ReSharper disable once ClassNeverInstantiated.Global
  [SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public class SMACodeImporterPlugin : SentrySMAPluginBase<SMACodeImporterPlugin>
    {
    #region Constructors

    /// <inheritdoc />
    public SMACodeImporterPlugin() : base("Enter your Sentry.io api key (strongly recommended)") { }
    public SMACodeImporterCfg Config { get; set; }
    public Dictionary<string, FileSystemWatcher> DatabaseWatchers { get; set; } = new Dictionary<string, FileSystemWatcher>();

    #endregion


    private void LoadConfig()
    {
      Config = Svc.Configuration.Load<SMACodeImporterCfg>() ?? new SMACodeImporterCfg();
    }


    #region Properties Impl - Public

    /// <inheritdoc />
    public override string Name => "SMACodeImporter";

    /// <inheritdoc />
    public override bool HasSettings => true;

    #endregion



    #region Methods Impl

    /// <inheritdoc />
    protected override void PluginInit()
    {
      LoadConfig();
      SetupDatabaseWatchers();
      Svc.HotKeyManager
   .RegisterGlobal(
     "OpenCodeImporter",
     "Open SMA Code Importer Window",
     HotKeyScopes.SMBrowser,
     new HotKey(Key.S, KeyModifiers.CtrlAltShift),
     OpenImportWdw
   );

    }

    private async void OpenImportWdw()
    {

      var outstandingExtracts = new List<Extract>();
      List<string> databases = Config.Databases.Split(',').Select(x => x.Trim()).ToList();
      if (databases == null || databases.Count == 0)
      {
        LogTo.Warning("Failed to OpenImportWdw because there are no databases in the config");
        return;
      }

      foreach (var database in databases)
      {
        if (!File.Exists(database))
        {
          LogTo.Warning($"Failed to add extracts from db {database} to import wdw because db does not exist");
          continue;
        }

        var db = new DataAccess(database);
        var results = await db.GetOutstandingExtractsAsync();
        if (results != null && results.Count > 0)
        {
          results.ForEach(x => x.database = database);
          outstandingExtracts.AddRange(results);
        }
      }

      Application.Current.Dispatcher.Invoke(() =>
      {
        var wdw = new ImportWdw(outstandingExtracts);
        wdw.ShowAndActivate();
      });

    }

    /// <summary>
    /// Creates a database watcher for each database in the config.
    /// </summary>
    private void SetupDatabaseWatchers()
    {
      List<string> databases = Config.Databases.Split(',').Select(x => x.Trim()).ToList();
      if (databases == null || databases.Count == 0)
      {
        LogTo.Warning("Databases Config variable is null or empty");
        return;
      }

      foreach (var database in databases)
      {
        if (!File.Exists(database))
        {
          LogTo.Warning($"Database {database} could not be found");
          continue;
        }
        else if (DatabaseWatchers.TryGetValue(database, out _))
        {
          LogTo.Warning($"The Config databases variable contains a duplicate database path");
          continue;
        }

        var watcher = CreateDBWatcher(database);
        if (watcher == null)
          continue;
        DatabaseWatchers[database] = watcher;
      }
    }

    /// <summary>
    /// Creates the database watcher and enables event raising.
    /// </summary>
    /// <param name="filepath"></param>
    /// <returns></returns>
    private FileSystemWatcher CreateDBWatcher(string filepath)
    {
      if (!File.Exists(filepath))
      {
        LogTo.Error($"Failed to CreateDBWatcher because database {filepath} does not exist");
        return null;
      }

      FileSystemWatcher watcher = new FileSystemWatcher();
      watcher.Path = Path.GetDirectoryName(filepath);
      watcher.Filter = Path.GetFileName(filepath);
      watcher.Changed += Watcher_Changed;
      watcher.EnableRaisingEvents = true;
      return watcher;
    }

    #endregion

    /// <summary>
    /// Create an SM Element from the latest extract.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Watcher_Changed(object sender, FileSystemEventArgs e)
    {
      string database = e.FullPath;
      LogTo.Debug($"File changed event raised for {database}");
      var db = new DataAccess(database);
      var extract = await db.GetLatestExtractAsync();
      if (extract == null)
      {
        LogTo.Debug($"No latest extract found in {database}");
        return;
      }
      await CreateSMExtract(extract, database);
    }

    public async Task CreateSMExtract(Extract extract, string database)
    {
      if (extract == null || !extract.IsValid())
      {
        LogTo.Warning("Attempted to CreateSMExtract using a null or invalid extract object");
        return;
      }

      if (!File.Exists(database))
      {
        LogTo.Error($"Failed to CreateSMExtract because database {database} does not exist");
        return;
      }

      var parentEl = Config.ImportAsChild
        ? Svc.SM.UI.ElementWdw.CurrentElement
        : Config.RootCodeElement;

      if (parentEl == null)
      {
        LogTo.Warning("Failed to CreateSMExtract because parentEl was null");
        return;
      }

      bool ret = false;
      var contents = new List<ContentBase>();
      string html = extract.highlightedCode;
      string comment = extract.comment;
      string combined = $"<pre>{html}</pre><BR><BR>{comment}<BR>";
      contents.Add(new TextContent(true, combined));

      // Generate extract
      if (contents.Count > 0)
      {
        string extractTitle = $"{extract.language} Code from {Path.GetFileName(extract.file)} in {extract.project}";

        ret = Svc.SM.Registry.Element.Add(
          out _,
          ElemCreationFlags.ForceCreate,
          new ElementBuilder(ElementType.Topic,
                             contents.ToArray())
            .WithParent(parentEl)
            .WithConcept(parentEl.Concept)
            .WithLayout("Article")
            .WithPriority(extract.priority)
            .WithReference(r =>
              r.WithTitle(extractTitle)
               .WithSource(extract.file))
               .DoNotDisplay()
        // TODO: Switch to DEFAULT CURRENT_TIMESTAMP in the sqlite db
        // TODO: Pycharm timestamp isn't working
        //.WithDate(extract.timestamp))
        // TODO: Make Display or DoNotDisplay a config option
        // Return to parent if do not display is active
        );

        if (ret)
        {
          LogTo.Debug("Code extract imported successfully");
          var db = new DataAccess(database);
          DatabaseWatchers[database].EnableRaisingEvents = false;
          await db.SetExportedAsync(extract.Id);
          DatabaseWatchers[database].EnableRaisingEvents = true;
        }
        else
        {
          LogTo.Error("Code Extract Import failed");
        }
      }
    }

    public override void ShowSettings()
    {
      ConfigurationWindow.ShowAndActivate(HotKeyManager.Instance, Config);
    }

    #region Methods

    #endregion
  }
}
