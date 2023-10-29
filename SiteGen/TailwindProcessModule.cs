using System.Diagnostics;
using Statiq.Common;

namespace SiteGen;

public class TailwindProcessModule : Module
{
    public static readonly string FileName = OperatingSystem.IsWindows() ? "cmd" : "bash";
    public static readonly string C = OperatingSystem.IsWindows() ? "/c" : "-c";
    
    protected override async Task<IEnumerable<IDocument>> ExecuteContextAsync(IExecutionContext context)
    {
        var docs = context.Inputs.Where(x => x.Destination.Extension == ".html").ToList();
        var css = context.Inputs.First(x => x.Destination == "_site.css");
        await docs.ParallelForEachAsync(async doc =>
            await File.WriteAllTextAsync(Path.Join("../Node", $"{Path.GetRandomFileName()}.tmp.html"),
                await doc.GetContentStringAsync()));
        
        // Get the default destination file
        var dst = context.FileSystem.GetOutputFile(css.Destination);
        var tmp = Path.GetTempFileName();
        static string FormatArgs(string s) => OperatingSystem.IsWindows() ? s : $"\"{s.Replace("\"", "\\\"")}\""; 
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = FileName,
                Arguments = $"{C} {FormatArgs($"npx tailwind build -c tailwind.config.js -i \"\" -o \"{tmp}\"")}",
                WorkingDirectory = Path.GetFullPath("../Node")
            }
        };

        process.Start();
        await process.WaitForExitAsync();
        foreach (var file in Directory.GetFiles("../Node", "*.tmp.html"))
        {
            File.Delete(file);
        }
        
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException("tailwind failed, have you restored the npm packages?");
        }

        await using var outSrc = File.OpenRead(tmp);
        await using var outDst = dst.OpenWrite();
        await outSrc.CopyToAsync(outDst);
        css = context.CreateDocument(css.Destination);

        return docs.Concat(css);
    }
}