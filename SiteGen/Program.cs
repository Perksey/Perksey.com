using Silk.NET.Statiq;
using SiteGen;
using Statiq.App;
using Statiq.Common;
using Statiq.Core;
using Statiq.Feeds;
using Statiq.Markdown;
using Statiq.Razor;
using Statiq.Yaml;

await Bootstrapper.Factory
    .CreateDefault(args)
    .ConfigureFileSystem
    (
        x =>
        {
            x.InputPaths.Clear();
            x.InputPaths.Add("../Content");
            x.OutputPath = "../Output";
        }
    )
    .BuildPipeline
    (
        "Static",
        builder => builder.WithInputReadFiles()
            .WithProcessModules(new CopyFiles("images/**/*", "*.pdf"))
    )
    .BuildPipeline
    (
        "Content",
        x => x.WithInputReadFiles("{**,!_theme/**}/*.cshtml", "**/*.md", "**/toc.json", "_site.css")
            .WithProcessModules
            (
                new ExtractFrontMatter(new ParseYaml()),
                new ForAllMatching()
                    .WithFilterPatterns("**/*.md")
                    .WithExecuteModules
                    (
                        new RenderMarkdown(),
                        new SetDestination(".html"),
                        new ProcessShortcodes(),
                        new InterpAsPost()
                    ),
                new AddPostLists(new Dictionary<string, List<string>>
                {
                    { "https://dotnet.github.io/Silk.NET/blog/feed.rss", new List<string> { "Perksey", "Dylan Perks" } }
                }),
                new ForAllMatching(true)
                    .WithFilterPatterns("blog/{**/*,!index.cshtml}")
                    .WithExecuteModules
                    (
                        new GenerateFeeds()
                            .WithItemLink
                            (
                                Config.FromDocument
                                (
                                    (y, z) => new Uri(z.GetLink(y, true))
                                )
                            )
                            .WithItemAuthor("Dylan Perks")
                            .WithItemPublished
                            (
                                Config.FromDocument<DateTime?>
                                (
                                    (y, _) => y.Get<DateTime>("DateTimePublished")
                                )
                            )
                            .WithItemImageLink
                            (
                                Config.FromDocument
                                (
                                    (y, _) => y.GetString("PreviewImage") is { } str && !string.IsNullOrWhiteSpace(str)
                                        ? Uri.IsWellFormedUriString(str, UriKind.Absolute)
                                            ? new Uri(str)
                                            : new Uri(new Uri("https://perksey.com"), str)
                                        : null
                                )
                            )
                            .WithAtomPath("blog/feed.atom")
                            .WithRssPath("blog/feed.rss")
                            .WithFeedTitle("Dylan Perks' Blog")
                            .WithFeedAuthor("Dylan Perks")
                            .WithFeedCopyright
                            (
                                $"Copyright (C) {DateTime.UtcNow.Year} Dylan Perks"
                            )
                            .AbsolutizeLinks(false)
                    ),
                new ForAllMatching(true)
                    .WithFilterPatterns("**/*.{md,html,cshtml}")
                    .WithExecuteModules
                    (
                        new RenderRazor(),
                        new ProcessShortcodes(),
                        new SetDestination(".html")
                    ),
                new TailwindProcessModule()
            )
            .WithOutputWriteFiles()
    )
    .AddShortcode<FancyImageShortCode>("FancyImage")
    .AddShortcode<CaptionShortCode>("Caption")
    .AddShortcode<InfoShortCode>("Info")
    .AddShortcode<WarningShortCode>("Warning")
    .AddSetting("Host", "perksey.com")
    .AddSetting("LinksUseHttps", true)
    .AddSetting("LinkHideExtensions", false)
    .RunAsync();