using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Highlight;
using Highlight.Engines;
using Highlight.Patterns;

namespace SuperMemoAssistant.Plugins.SMACodeImporter
{
  public class MyHtmlEngine : HtmlEngine
  {
    /// <summary>
    /// Skip the html.encode step in the base class because it mangles brackets.
    /// </summary>
    /// <returns></returns>
    public new string PreHighlight(Definition definition, string input)
    {
      if (definition == null)
      {
        throw new ArgumentNullException("definition");
      }
      return input;
    }
  }
}
