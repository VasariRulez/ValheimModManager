namespace ValheimModManager.App.Controls;

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

/// <summary>
/// Controllo compatto e leggero per la visualizzazione formattata di testo Markdown (Changelog, note di rilascio).
/// Supporta intestazioni (#, ##, ###, ####), elenchi puntati (- o *), testo in grassetto (**testo**),
/// codice inline (`codice`), e separatori orizzontali (---).
/// </summary>
public class MarkdownDocumentView : StackPanel
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownDocumentView, string?>(nameof(Markdown));

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    static MarkdownDocumentView()
    {
        MarkdownProperty.Changed.AddClassHandler<MarkdownDocumentView>((view, _) => view.RenderMarkdown());
    }

    public MarkdownDocumentView()
    {
        Spacing = 6;
    }

    private void RenderMarkdown()
    {
        Children.Clear();

        var text = Markdown;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            // Linea vuota -> piccolo spazio o salta
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var trimmed = line.Trim();

            // Separatore orizzontale
            if (trimmed == "---" || trimmed == "***" || trimmed == "___")
            {
                Children.Add(new Separator
                {
                    Background = Brush.Parse("#3f3f46"),
                    Margin = new Thickness(0, 6, 0, 6),
                    Height = 1
                });
                continue;
            }

            // Intestazioni: ### o #### o ## o #
            if (trimmed.StartsWith("#### "))
            {
                var headerText = trimmed[5..].Trim();
                var tb = new TextBlock
                {
                    Text = headerText,
                    FontWeight = FontWeight.Bold,
                    FontSize = 12,
                    Foreground = Brush.Parse("#a1a1aa"),
                    Margin = new Thickness(0, 8, 0, 2)
                };
                Children.Add(tb);
                continue;
            }

            if (trimmed.StartsWith("### "))
            {
                var headerText = trimmed[4..].Trim();
                var tb = new TextBlock
                {
                    Text = headerText,
                    FontWeight = FontWeight.Bold,
                    FontSize = 13,
                    Foreground = Brush.Parse("#93c5fd"),
                    Margin = new Thickness(0, 10, 0, 4)
                };
                Children.Add(tb);
                continue;
            }

            if (trimmed.StartsWith("## "))
            {
                var headerText = trimmed[3..].Trim();
                var tb = new TextBlock
                {
                    Text = headerText,
                    FontWeight = FontWeight.Bold,
                    FontSize = 14,
                    Foreground = Brush.Parse("#60a5fa"),
                    Margin = new Thickness(0, 12, 0, 4)
                };
                Children.Add(tb);
                continue;
            }

            if (trimmed.StartsWith("# "))
            {
                var headerText = trimmed[2..].Trim();
                var tb = new TextBlock
                {
                    Text = headerText,
                    FontWeight = FontWeight.Bold,
                    FontSize = 16,
                    Foreground = Brush.Parse("#f4f4f5"),
                    Margin = new Thickness(0, 14, 0, 6)
                };
                Children.Add(tb);
                continue;
            }

            // Elementi di elenco puntato (- o *)
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* "))
            {
                var bulletContent = trimmed[2..].Trim();
                var listItem = CreateListItemControl(bulletContent);
                Children.Add(listItem);
                continue;
            }

            // Paragrafo normale
            var paragraph = CreateParagraphControl(line);
            Children.Add(paragraph);
        }
    }

    private static Control CreateListItemControl(string content)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            Margin = new Thickness(4, 2, 0, 2)
        };

        var bullet = new TextBlock
        {
            Text = "•",
            Foreground = Brush.Parse("#60a5fa"),
            FontWeight = FontWeight.Bold,
            FontSize = 12,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Top
        };
        Grid.SetColumn(bullet, 0);

        var textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            LineHeight = 18,
            VerticalAlignment = VerticalAlignment.Center
        };
        PopulateInlines(textBlock, content);
        Grid.SetColumn(textBlock, 1);

        grid.Children.Add(bullet);
        grid.Children.Add(textBlock);
        return grid;
    }

    private static TextBlock CreateParagraphControl(string content)
    {
        var textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            LineHeight = 18,
            Margin = new Thickness(0, 2, 0, 2)
        };
        PopulateInlines(textBlock, content);
        return textBlock;
    }

    /// <summary>
    /// Esegue il parsing di elementi inline come grassetto (**testo**) e codice (`codice`).
    /// </summary>
    public static void PopulateInlines(TextBlock textBlock, string text)
    {
        textBlock.Inlines?.Clear();

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var i = 0;
        var len = text.Length;

        while (i < len)
        {
            // Verifica grassetto: **testo**
            if (i + 1 < len && text[i] == '*' && text[i + 1] == '*')
            {
                var closing = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (closing != -1)
                {
                    var boldText = text.Substring(i + 2, closing - (i + 2));
                    textBlock.Inlines?.Add(new Run
                    {
                        Text = boldText,
                        FontWeight = FontWeight.Bold,
                        Foreground = Brush.Parse("#f4f4f5")
                    });
                    i = closing + 2;
                    continue;
                }
            }

            // Verifica codice inline: `codice`
            if (text[i] == '`')
            {
                var closing = text.IndexOf('`', i + 1);
                if (closing != -1)
                {
                    var codeText = text.Substring(i + 1, closing - (i + 1));
                    textBlock.Inlines?.Add(new Run
                    {
                        Text = $" {codeText} ",
                        FontFamily = new FontFamily("Consolas,Monospace"),
                        Foreground = Brush.Parse("#38bdf8"),
                        FontWeight = FontWeight.Medium
                    });
                    i = closing + 1;
                    continue;
                }
            }

            // Testo normale fino al prossimo token (** o `)
            var nextBold = text.IndexOf("**", i, StringComparison.Ordinal);
            var nextCode = text.IndexOf('`', i);

            int nextToken;
            if (nextBold != -1 && nextCode != -1)
            {
                nextToken = Math.Min(nextBold, nextCode);
            }
            else if (nextBold != -1)
            {
                nextToken = nextBold;
            }
            else if (nextCode != -1)
            {
                nextToken = nextCode;
            }
            else
            {
                nextToken = len;
            }

            var plainText = text.Substring(i, nextToken - i);
            textBlock.Inlines?.Add(new Run
            {
                Text = plainText,
                Foreground = Brush.Parse("#d4d4d8")
            });
            i = nextToken;
        }
    }
}
