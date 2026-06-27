using SecShare.Console.Enums;
using SecShare.Console.Models.Upload;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace SecShare.Console.Services.Upload;

public static class UploadShareOutputWriter
{
    public static void Write(
        UploadPackage package,
        UploadShareLinks links,
        string mode,
        string expires,
        int downloads
    )
    {
        var lines = UploadShareOutputFormatter.CreateLines(
            package,
            links,
            mode,
            expires,
            downloads
        );
        var panel = new Panel(new Rows(CreateRenderables(lines)))
            .Header("[green]SecShare[/]")
            .Border(BoxBorder.Square)
            .BorderColor(Color.Green);

        WritePanelWithoutBreakingLongValues(panel, lines);
    }

    private static void WritePanelWithoutBreakingLongValues(
        Panel panel,
        IReadOnlyList<UploadShareOutputLine> lines
    )
    {
        var originalWidth = AnsiConsole.Console.Profile.Width;
        var requiredWidth = GetRequiredPanelWidth(lines);

        try
        {
            if (AnsiConsole.Console.Profile.Width < requiredWidth)
            {
                AnsiConsole.Console.Profile.Width = requiredWidth;
            }

            AnsiConsole.Write(panel);
        }
        finally
        {
            AnsiConsole.Console.Profile.Width = originalWidth;
        }
    }

    private static int GetRequiredPanelWidth(IReadOnlyList<UploadShareOutputLine> lines)
    {
        var maxLineLength = lines
            .Select(line => line.Segments.Sum(segment => segment.Text.Length))
            .DefaultIfEmpty(0)
            .Max();

        return maxLineLength + 4;
    }

    private static IEnumerable<IRenderable> CreateRenderables(IReadOnlyList<UploadShareOutputLine> lines)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            yield return new UploadShareOutputLineRenderable(lines[index]);

            if (index is 2 or 4 or 7)
            {
                yield return new Rule().RuleStyle(new Style(Color.Grey));
            }
        }
    }

    private sealed class UploadShareOutputLineRenderable(UploadShareOutputLine line) : IRenderable
    {
        public Measurement Measure(RenderOptions options, int maxWidth)
        {
            var lineLength = line.Segments.Sum(segment => segment.Text.Length);

            return new Measurement(Math.Min(lineLength, maxWidth), lineLength);
        }

        public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            foreach (var segment in line.Segments)
            {
                yield return new Segment(segment.Text, GetStyle(segment.Style), link: null);
            }
        }
    }

    private static Style GetStyle(UploadShareOutputStyle style)
    {
        return style switch
        {
            UploadShareOutputStyle.Success => new Style(Color.Green),
            UploadShareOutputStyle.Header => new Style(Color.Cyan1),
            UploadShareOutputStyle.Label => new Style(Color.Cyan1),
            UploadShareOutputStyle.Value => new Style(Color.Yellow),
            UploadShareOutputStyle.Secondary => new Style(Color.Grey),
            _ => Style.Plain
        };
    }
}
