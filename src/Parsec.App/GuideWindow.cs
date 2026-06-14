using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Parsec.App;

/// <summary>
/// A non-modal, read-only companion window that renders a <see cref="GuideDocument"/>
/// for the active fractal. Built in code to match the ParameterPanel pattern (a
/// UserControl/Window wrapping a ScrollViewer scrolls reliably). Content is
/// TextBlock/SelectableTextBlock only (no TextBox), so it can be read and copied
/// but never edited. Escape closes it.
/// </summary>
public sealed class GuideWindow : Window
{
    private static readonly IBrush Bg = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x26));
    private static readonly IBrush Dim = new SolidColorBrush(Color.FromRgb(0x9a, 0x9a, 0xb0));
    private static readonly IBrush Body = new SolidColorBrush(Color.FromRgb(0xd0, 0xd0, 0xd8));
    private static readonly IBrush Accent = new SolidColorBrush(Color.FromRgb(0xa0, 0xc0, 0xe0));

    private readonly StackPanel _root;

    public GuideWindow()
    {
        Width = 480;
        Height = 720;
        Background = Bg;
        Title = "Guide";

        _root = new StackPanel { Margin = new Thickness(16), Spacing = 6 };
        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _root,
        };

        KeyDown += (_, e) => { if (e.Key == Key.Escape) Close(); };
    }

    /// <summary>Rebuild the window body for a new document (used on open and on
    /// fractal/formula switch while open).</summary>
    public void Populate(GuideDocument doc)
    {
        Title = $"Guide: {doc.Title}";
        _root.Children.Clear();

        _root.Children.Add(new SelectableTextBlock
        {
            Text = doc.Title,
            Foreground = Body,
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        foreach (var block in doc.Blocks)
        {
            switch (block)
            {
                case GuideBlock.Heading h:
                    _root.Children.Add(new SelectableTextBlock
                    {
                        Text = h.Text.ToUpperInvariant(),
                        Foreground = Accent,
                        FontSize = 13,
                        FontWeight = FontWeight.SemiBold,
                        Margin = new Thickness(0, 14, 0, 2),
                    });
                    break;

                case GuideBlock.Paragraph p:
                    _root.Children.Add(new SelectableTextBlock
                    {
                        Text = p.Text,
                        Foreground = Body,
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 4),
                    });
                    break;

                case GuideBlock.SettingGroupHeading g:
                    _root.Children.Add(new SelectableTextBlock
                    {
                        Text = g.Group.ToUpperInvariant(),
                        Foreground = Dim,
                        FontSize = 11,
                        FontWeight = FontWeight.SemiBold,
                        Margin = new Thickness(0, 10, 0, 2),
                    });
                    break;

                case GuideBlock.SettingDefinition d:
                    _root.Children.Add(BuildDefinition(d));
                    break;
            }
        }
    }

    private Control BuildDefinition(GuideBlock.SettingDefinition d)
    {
        var box = new StackPanel { Spacing = 1, Margin = new Thickness(0, 2, 0, 4) };

        var header = new WrapPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(new SelectableTextBlock
        {
            Text = d.Name + "  ",
            Foreground = Body,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
        });
        header.Children.Add(new SelectableTextBlock
        {
            Text = d.Range,
            Foreground = Dim,
            FontSize = 11,
            FontFamily = new FontFamily("monospace"),
            VerticalAlignment = VerticalAlignment.Center,
        });
        box.Children.Add(header);

        if (!string.IsNullOrEmpty(d.Note))
            box.Children.Add(new SelectableTextBlock
            {
                Text = d.Note,
                Foreground = Body,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
            });

        return box;
    }
}
