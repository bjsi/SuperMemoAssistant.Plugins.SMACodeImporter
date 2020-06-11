using Forge.Forms;
using SuperMemoAssistant.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SuperMemoAssistant.Plugins.SMACodeImporter.UI
{

  public class Extracts : ObservableCollection<Extract>
  {
  }

  /// <summary>
  /// Interaction logic for ImportWdw.xaml
  /// </summary>
  public partial class ImportWdw : Window
  {
    public ImportWdw(List<Extract> extracts)
    {
      if (extracts == null || extracts.Count == 0)
      {
        Forge.Forms.Show.Window().For(new Alert("No code extracts available"));
        return;
      }

      InitializeComponent();

      Extracts _extracts = (Extracts)this.Resources["extracts"];
      foreach (var extract in extracts)
      {
        _extracts.Add(extract);
      }
    }

    private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
    {
      
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private async void Button_Click_1(object sender, RoutedEventArgs e)
    {
      ICollectionView extracts = CollectionViewSource.GetDefaultView(DG1.ItemsSource);
      List<Extract> ToImport = extracts.Cast<Extract>().Where(e => e.ToImport).ToList();
      
      foreach (var obj in ToImport)
      {
        var extract = (Extract)obj;
        await Svc<SMACodeImporterPlugin>.Plugin.CreateSMExtract(extract, extract.database);
      }
    }
  }
}
