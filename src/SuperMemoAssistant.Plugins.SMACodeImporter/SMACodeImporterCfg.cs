using Forge.Forms.Annotations;
using Newtonsoft.Json;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.UI.Configuration;
using SuperMemoAssistant.Services.UI.Configuration.ElementPicker;
using SuperMemoAssistant.Sys.ComponentModel;
using System;
using System.ComponentModel;

namespace SuperMemoAssistant.Plugins.SMACodeImporter
{
  [Form(Mode = DefaultFields.None)]
  [Title("Dictionary Settings",
     IsVisible = "{Env DialogHostContext}")]
  [DialogAction("cancel",
            "Cancel",
            IsCancel = true)]
  [DialogAction("save",
            "Save",
            IsDefault = true,
            Validates = true)]
  public class SMACodeImporterCfg : CfgBase<SMACodeImporterCfg>, INotifyPropertyChangedEx, IElementPickerCallback
  {

    [Field(Name = "Database Paths (comma separated list)")]
    public string Databases { get; set; } = @"C:\Users\james\Desktop\SMACodeExtracts.db,
                                              C:\Users\james\.vscode\extensions\ms-vscode-remote.remote-wsl-0.44.2\SMACodeExtracts.db,
                                              C:\Users\james\Desktop\VSExtracts.db,
                                              C:\Users\james\Desktop\vim_extracts.db";

    [Field(Name = "Auto Import?")]
    public bool AutoImport { get; set; } = true;

    [Field(Name = "Import as child of the current element?")]
    public bool ImportAsChild { get; set; } = true;

    [Field(Name = "Display newly created elements after creation?")]
    public bool DisplayAfterCreation { get; set; } = true;

    [JsonIgnore]
    [Action(ElementPicker.ElementPickerAction,
        "Browse",
        Placement = Placement.Inline)]
    [Field(Name = "Root Element",
       IsReadOnly = true)]
    public string ElementField
    {
      // ReSharper disable once ValueParameterNotUsed
      set
      {
        /* empty */
      }
      get => RootCodeElement == null
        ? "N/A"
        : RootCodeElement.ToString();
    }


    [JsonIgnore]
    public bool IsChanged { get; set; }

    public override string ToString()
    {
      return "Code Importer";
    }

    public int RootCodeElementId { get; set; }


    [JsonIgnore]
    public IElement RootCodeElement => Svc.SM.Registry.Element[RootCodeElementId <= 0 ? 1 : RootCodeElementId];

    public void SetElement(IElement elem)
    {
      RootCodeElementId = elem.Id;

      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElementField)));
    }

    public event PropertyChangedEventHandler PropertyChanged;
  }

}
