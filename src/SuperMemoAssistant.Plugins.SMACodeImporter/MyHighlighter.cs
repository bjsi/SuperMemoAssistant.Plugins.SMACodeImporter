using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anotar.Serilog;
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

  public static class CodeHighlighter
  {
    private static Dictionary<string, string> languageMap = new Dictionary<string, string>()
    {
      { "csharp", "C#" },
      { "haskell", "Haskell" },
      { "python", "Python" },
      { "javascript", "JavaScript" },
      { "c", "C" },
      { "c++", "C++" },
      { "c/c++", "C++" }
    };
    private static Highlighter highlighter = new Highlighter(new MyHtmlEngine());

    public static string Highlight(string input, string language)
    {
      if (string.IsNullOrEmpty(language) || string.IsNullOrEmpty(input))
      {
        LogTo.Debug("Failed to highlight code because language or input was null or empty");
        return input;
      }

      if (!languageMap.TryGetValue(language, out var langCode))
      {
        LogTo.Debug($"Failed to highlight code with unrecognized language {language}");
        return input;
      }
      return highlighter.Highlight(langCode, input);
    }
  }
}
