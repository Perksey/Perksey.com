using System.Globalization;
using HtmlAgilityPack;
using Statiq.Common;

namespace SiteGen;

public class InterpAsPost : Module
{
    private const string Alert = """
                                 <a href="{0}">
                                 <div class="bg-blue-600 text-sm border border-white text-white px-4 py-3 rounded relative" role="alert">
                                   <strong class="font-bold">This post was originally written for another blog, click here to view the original.</strong>
                                 </div>
                                 </a>
                                 """;

    protected override async Task<IEnumerable<IDocument>> ExecuteInputAsync(IDocument input, IExecutionContext context)
    {
        return input.Clone(input.Concat(new KeyValuePair<string, object>("IsPost", true)), context.GetContentProvider(
            await Wrap(await input.GetContentStringAsync(),
                input.FirstOrDefault(y => y.Key == "ExternalPost").Value as string))).Yield();
    }

    internal static async Task<string> Wrap(string content, string? externalUrl)
    {
        var str = new StringWriter();
        await str.WriteLineAsync(
            "<article class=\"prose prose-lg prose-zinc dark:prose-invert md:prose-xl prose-h1:tracking-tight prose-a:text-blue-500 prose-a:decoration-2 prose-a:transition-colors prose-a:ease-in-out hover:prose-a:text-blue-400 prose-pre:bg-zinc-900 dark:hover:prose-a:text-blue-600\">");
        if (externalUrl is not null)
        {
            await str.WriteLineAsync(string.Format(Alert, externalUrl));
        }

        // TODO <time dateTime={post.date}>{post.formattedDate}</time> &bull;{" "}{post.readingTime} min read
        await str.WriteLineAsync(content);
        await str.WriteLineAsync("</article>");
        return str.ToString();
    }
}