using System.Globalization;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Xml;
using HtmlAgilityPack;
using Statiq.Common;
using Statiq.Feeds.Syndication.Rss;

namespace SiteGen;

public class AddPostLists(Dictionary<string, List<string>> rssToAuthorNameMaps) : Module
{
    private HttpClient _client = new();

    protected override async Task<IEnumerable<IDocument>> ExecuteContextAsync(IExecutionContext context)
    {
        var newDocs = new List<IDocument>();
        var allPosts = new List<Dictionary<string, object>>();
        foreach (var (url, authors) in rssToAuthorNameMaps)
        {
            var feed = SyndicationFeed.Load(XmlReader.Create(await _client.GetStreamAsync(url)));
            var prefix = Regex.Replace(feed.Title.Text, "[^A-Za-z0-9]", "-").ToLower();
            foreach (var post in feed.Items)
            {
                if (post.Authors.Any(x => authors.Contains(x.Email)))
                {
                    var uri = post.Links.First(x => x.RelationshipType == "alternate").Uri;
                    var path = $"blog/{prefix}-{uri.Segments.Last()}";
                    var str = post.ElementExtensions.First(x => x.OuterName == "encoded").GetReader()
                        .ReadElementString();
                    var html = new HtmlDocument();
                    html.LoadHtml(str);
                    var thisDict = new Dictionary<string, object>
                    {
                        { "IsPost", true },
                        { "ExternalPost", uri.ToString() },
                        {
                            "PreviewImage",
                            post.Links.FirstOrDefault(x => x.RelationshipType == "enclosure")?.Uri.ToString() ?? ""
                        },
                        { "Title", post.Title.Text },
                        { "DateTimePublished", post.PublishDate.DateTime },
                        { "PostUrl", $"/{path}" },
                        {
                            "FirstParagraph",
                            html.DocumentNode.SelectNodes("//p[not(@id) and not(@class)]")?.FirstOrDefault()
                                ?.InnerText ?? ""
                        }
                    };
                    foreach (var img in html.DocumentNode.SelectNodes("//img") ?? Enumerable.Empty<HtmlNode>())
                    {
                        var src = img.GetAttributeValue("src", null) ?? throw new("Needs src attr!");
                        if (!Uri.IsWellFormedUriString(src, UriKind.Absolute))
                        {
                            img.SetAttributeValue("src", new Uri(uri, src).ToString());
                        }
                    }

                    foreach (var a in html.DocumentNode.SelectNodes("*//*[@href]") ?? Enumerable.Empty<HtmlNode>())
                    {
                        var href = a.GetAttributeValue("href", null);
                        if (href is null)
                        {
                            continue;
                        }

                        if (!Uri.IsWellFormedUriString(href, UriKind.Absolute))
                        {
                            a.SetAttributeValue("href", new Uri(uri, href).ToString());
                        }
                    }

                    foreach (var collapsible in html.DocumentNode.SelectNodes("//div[@class='collapse']") ??
                                                Enumerable.Empty<HtmlNode>())
                    {
                        var id = collapsible.GetAttributeValue("id", null) ?? throw new("Must have id!");
                        var expander = html.DocumentNode.SelectNodes($"//a[@href='#{id}']").First();
                        collapsible.Name = "details";
                        collapsible.InsertBefore(HtmlNode.CreateNode($"<summary>{expander.InnerText}</summary>"),
                            collapsible.ChildNodes.First());
                        collapsible.SetAttributeValue("class", "");
                        expander.ParentNode.RemoveChild(expander);
                    }

                    str = await InterpAsPost.Wrap(html.DocumentNode.InnerHtml, uri.ToString());
                    newDocs.Add(context.CreateDocument(
                        Path.GetFullPath(path, Path.GetFullPath(context.FileSystem.InputPaths.First().FullPath)), path,
                        thisDict,
                        context.GetContentProvider(str)));
                    allPosts.Add(thisDict);
                }
            }
        }

        return newDocs.Concat(context.Inputs.Select(x =>
        {
            if (!x.Get<bool>("IsPost"))
            {
                return x.Clone(x.Concat(new KeyValuePair<string, object>("AllPosts", allPosts)));
            }

            var html = new HtmlDocument();
            using var reader = x.GetContentTextReader();
            html.LoadHtml(reader.ReadToEnd());
            var dict = new Dictionary<string, object>
            {
                { "PostUrl", $"/{x.Destination}" },
                {
                    "FirstParagraph",
                    html.DocumentNode.SelectNodes("//p[not(@id) and not(@class)]")?.FirstOrDefault()?.InnerText ??
                    ""
                },
                {
                    "DateTimePublished",
                    x.TryGetValue("DateTimePublished", out var val) && val is string str &&
                    DateTime.TryParseExact(str, "dd/MM/yyyy HH:mm", null, DateTimeStyles.AssumeUniversal,
                        out var pub)
                        ? pub
                        : val is DateTime dt
                            ? dt
                            : DateTime.MinValue
                }
            };
            allPosts.Add(x.Where(y => y.Key is not "DateTimePublished").Concat(dict)
                .ToDictionary(y => y.Key, y => y.Value));

            return x.Clone(x.Concat(new KeyValuePair<string, object>("AllPosts", allPosts)));
        }));
    }
}