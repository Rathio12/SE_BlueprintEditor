using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;

var docs = Path.GetFullPath(args.Length > 0 ? args[0] : "docs");
var baseUrl = args.Length > 1 ? args[1] : "https://rathio12.github.io/SE_BlueprintEditor/";
const string Repo = "https://github.com/Rathio12/SE_BlueprintEditor";
const string SiteName = "SE Blueprint Inspector";

var order = new[]
{
    "Home", "How-It-Works", "Reading-Game-Data", "Reading-Blueprints", "Mods-Weapons-and-Cargo",
    "Cost-Calculation", "Limits-and-Profiles", "Building-and-Releasing", "FAQ",
};

var wikiDir = Path.Combine(docs, "wiki");
var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
var pages = Directory.GetFiles(wikiDir, "*.md")
    .Select(f => new
    {
        Slug = Path.GetFileNameWithoutExtension(f),
        Markdown = File.ReadAllText(f),
        Modified = File.GetLastWriteTimeUtc(f),
    })
    .Select(p => new Page(p.Slug, TitleOf(p.Markdown, p.Slug), DescriptionOf(p.Markdown), p.Markdown, p.Modified))
    .OrderBy(p => Array.IndexOf(order, p.Slug) is var i && i >= 0 ? i : int.MaxValue)
    .ThenBy(p => p.Slug)
    .ToList();

foreach (var page in pages)
{
    var body = RewriteLinks(Markdown.ToHtml(page.Markdown, pipeline));
    var nav = string.Join("\n", pages.Select(p =>
        $"        <a href=\"{p.Slug}.html\"{(p == page ? " class=\"sel\" aria-current=\"page\"" : "")}>{E(p.Title)}</a>"));
    var url = $"{baseUrl}wiki/{page.Slug}.html";
    var title = page.Slug == "Home" ? $"Wiki — {SiteName}" : $"{page.Title} — {SiteName} Wiki";
    File.WriteAllText(Path.Combine(wikiDir, page.Slug + ".html"), Layout(title, page.Description, url, "../", $"""
    <div class="layout">
      <nav class="side" aria-label="Wiki pages">
        <div class="side-title">WIKI</div>
{nav}
      </nav>
      <main class="article">
        <article>
{body}
        </article>
      </main>
    </div>
"""), new UTF8Encoding(false));
}

File.WriteAllText(Path.Combine(wikiDir, "wiki.css"), Css, new UTF8Encoding(false));

File.WriteAllText(Path.Combine(docs, "404.html"), Layout($"Page not found — {SiteName}",
    "This page does not exist.", null, BasePath(baseUrl), $"""
    <main class="article notfound">
      <h1>Page not found</h1>
      <p>The page you were looking for does not exist.</p>
      <p><a href="{BasePath(baseUrl)}">Open SE Blueprint Inspector</a> · <a href="{BasePath(baseUrl)}wiki/Home.html">Read the wiki</a></p>
    </main>
""", noIndex: true), new UTF8Encoding(false));

var sitemap = new StringBuilder();
sitemap.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
sitemap.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
sitemap.AppendLine($"  <url><loc>{baseUrl}</loc><lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod></url>");
foreach (var p in pages)
    sitemap.AppendLine($"  <url><loc>{baseUrl}wiki/{p.Slug}.html</loc><lastmod>{p.Modified:yyyy-MM-dd}</lastmod></url>");
sitemap.AppendLine("</urlset>");
File.WriteAllText(Path.Combine(docs, "sitemap.xml"), sitemap.ToString(), new UTF8Encoding(false));

File.WriteAllText(Path.Combine(docs, "robots.txt"), $"User-agent: *\nAllow: /\n\nSitemap: {baseUrl}sitemap.xml\n", new UTF8Encoding(false));

Console.WriteLine($"Generated {pages.Count} wiki pages, sitemap.xml, robots.txt and 404.html in {docs}");
return 0;

string Layout(string title, string description, string? canonical, string root, string content, bool noIndex = false) => $"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>{E(title)}</title>
  <meta name="description" content="{E(description)}" />
{(noIndex ? "  <meta name=\"robots\" content=\"noindex\" />" : $"""
  <link rel="canonical" href="{canonical}" />
  <meta property="og:type" content="article" />
  <meta property="og:site_name" content="{SiteName}" />
  <meta property="og:title" content="{E(title)}" />
  <meta property="og:description" content="{E(description)}" />
  <meta property="og:url" content="{canonical}" />
  <meta property="og:image" content="{baseUrl}images/blueprints.png" />
  <meta name="twitter:card" content="summary_large_image" />
""")}
  <meta name="theme-color" content="#070D11" />
  <link rel="icon" type="image/png" href="{root}favicon.png" />
  <link rel="stylesheet" href="{(noIndex ? root + "wiki/" : "")}wiki.css" />
</head>
<body>
  <header class="top">
    <a class="logo" href="{root}"><img src="{root}app-icon.svg" width="28" height="28" alt="SE Blueprint Inspector logo" /><span>SE BLUEPRINT <b>INSPECTOR</b></span></a>
    <nav class="links" aria-label="Site">
      <a href="{root}">Open the app</a>
      <a href="{root}wiki/Home.html">Wiki</a>
      <a href="{Repo}/releases/latest">Download</a>
      <a href="{Repo}">GitHub</a>
    </nav>
  </header>
{content}
  <footer class="foot">
    {SiteName} · free and open source (MIT OR GPL-3.0) · Space Engineers © Keen Software House; not affiliated with or endorsed by Keen Software House.
  </footer>
</body>
</html>
""";

static string BasePath(string url) => new Uri(url).AbsolutePath;

static string E(string s) => WebUtility.HtmlEncode(s);

static string TitleOf(string md, string slug) =>
    md.Split('\n').FirstOrDefault(l => l.StartsWith("# "))?[2..].Trim() ?? slug.Replace('-', ' ');

static string DescriptionOf(string md)
{
    var paragraph = md.Replace("\r", "").Split("\n\n")
        .Select(b => b.Trim())
        .FirstOrDefault(b => b.Length > 0 && !b.StartsWith('#') && !b.StartsWith('|') && !b.StartsWith("```") && !b.StartsWith('>') && !b.StartsWith('-') && !b.StartsWith('*'))
        ?? "Documentation for SE Blueprint Inspector, a Space Engineers blueprint viewer and cost calculator.";
    var text = Regex.Replace(paragraph, @"\[([^\]]+)\]\([^)]+\)", "$1");
    text = Regex.Replace(text, @"[`*_]", "").Replace('\n', ' ').Trim();
    return text.Length <= 158 ? text : text[..155].TrimEnd() + "…";
}

string RewriteLinks(string html) => Regex.Replace(html, "href=\"([^\"]+)\"", m =>
{
    var href = m.Groups[1].Value;
    if (href.StartsWith('#') || href.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return m.Value;
    if (href.StartsWith("../../", StringComparison.Ordinal)) return $"href=\"{Repo}/blob/main/{href[6..]}\"";
    var parts = href.Split('#', 2);
    if (parts[0].EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        return $"href=\"{parts[0][..^3]}.html{(parts.Length > 1 ? "#" + parts[1] : "")}\"";
    return m.Value;
});

record Page(string Slug, string Title, string Description, string Markdown, DateTime Modified);

static partial class Program
{
    const string Css = """
:root { --bg0:#070D11; --bg1:#0B151B; --panel:#0F1E26; --border:#24485A; --hi:#4FA9CF; --accent:#46B4E6; --text:#DCE8EE; --dim:#8EA6B2; --faint:#5D7682; --row:#14303C;
  --header:"Bahnschrift SemiCondensed","Bahnschrift","DIN Alternate","Roboto Condensed","Arial Narrow",sans-serif; --body:"Segoe UI",system-ui,-apple-system,"Helvetica Neue",Arial,sans-serif; }
* { box-sizing: border-box; }
html, body { margin: 0; background: var(--bg1); color: var(--text); font: 15px/1.6 var(--body); }
body { background-image: linear-gradient(rgba(14,42,56,.6) 1px, transparent 1px), linear-gradient(90deg, rgba(14,42,56,.6) 1px, transparent 1px); background-size: 32px 32px; min-height: 100vh; display: flex; flex-direction: column; }
a { color: var(--accent); }
.top { display: flex; align-items: center; justify-content: space-between; gap: 16px; padding: 10px 20px; background: var(--bg0); border-bottom: 1px solid var(--border); flex-wrap: wrap; }
.logo { display: flex; align-items: center; gap: 10px; text-decoration: none; color: var(--text); font: 700 16px var(--header); }
.logo b { color: var(--accent); font-weight: 400; }
.links { display: flex; gap: 18px; font-family: var(--header); font-weight: 600; }
.links a { text-decoration: none; color: var(--dim); }
.links a:hover { color: var(--text); }
.layout { display: grid; grid-template-columns: 250px 1fr; gap: 16px; max-width: 1180px; width: 100%; margin: 20px auto; padding: 0 16px; flex: 1; }
.side { background: rgba(15,30,38,.9); border: 1px solid var(--border); padding: 10px 0; align-self: start; position: sticky; top: 16px; }
.side-title { font: 700 13px var(--header); color: var(--accent); padding: 4px 14px 8px; }
.side a { display: block; padding: 7px 14px; color: var(--text); text-decoration: none; border-left: 3px solid transparent; font-weight: 600; font-size: 14px; }
.side a:hover { background: #0F2A36; }
.side a.sel { background: #143846; border-left-color: var(--accent); }
.article { background: rgba(15,30,38,.92); border: 1px solid var(--border); padding: 26px 34px; min-width: 0; }
.article h1 { font: 600 32px var(--header); margin: 0 0 12px; }
.article h2 { font: 700 19px var(--header); color: var(--accent); margin: 30px 0 10px; text-transform: uppercase; }
.article h3 { font: 700 16px var(--header); margin: 22px 0 8px; }
.article table { border-collapse: collapse; width: 100%; margin: 12px 0; font-size: 14px; }
.article th { text-align: left; font: 600 13px var(--header); color: var(--accent); background: var(--panel); border-bottom: 1px solid var(--border); padding: 8px 10px; }
.article td { border-bottom: 1px solid var(--row); padding: 7px 10px; vertical-align: top; }
.article code { font-family: Consolas, "Cascadia Mono", monospace; font-size: 13px; background: #0A1A22; padding: 1px 5px; border: 1px solid var(--row); }
.article pre { background: #0A1A22; border: 1px solid var(--border); padding: 14px 16px; overflow-x: auto; }
.article pre code { border: 0; padding: 0; background: none; }
.article blockquote { margin: 12px 0; padding: 8px 14px; border-left: 3px solid var(--accent); color: var(--dim); background: #0A1A22; }
.notfound { max-width: 700px; margin: 60px auto; flex: 1; }
.foot { padding: 14px 20px; border-top: 1px solid var(--border); background: var(--bg0); color: var(--faint); font-size: 12.5px; text-align: center; }
@media (max-width: 860px) { .layout { grid-template-columns: 1fr; } .side { position: static; } .article { padding: 18px; } }
""";
}
