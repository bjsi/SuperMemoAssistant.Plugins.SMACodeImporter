using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperMemoAssistant.Plugins.SMACodeImporter
{
  public class Extract
  {
    public long Id { get; set; }
    public DateTime timestamp { get; set; }
    public string selectedCode { get; set; }
    public string comment { get; set; }
    public double priority { get; set; }
    public bool exported { get; set; }
    public string file { get; set; }
    public string language { get; set; }
    public string project { get; set; }

    public bool IsValid()
    {
      bool ret = true;
      if (string.IsNullOrEmpty(selectedCode))
      {
        ret = false;
      }
      else if (priority < 0 || priority > 100)
      {
        ret = false;
      }
      else if (exported)
      {
        ret = false;
      }

      return ret;
    }
  }
}
