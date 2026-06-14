using System;
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
/// but never edited. Escape closes it; Ctrl+Plus / Ctrl+Minus (or Ctrl+mouse wheel)
/// zoom the text and Ctrl+0 resets the zoom.
/// </summary>
public sealed class GuideWindow : Window
{
    private static readonly IBrush Bg = new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x26));
    private static readonly IBrush Dim = new SolidColorBrush(Color.FromRgb(0x9a, 0x9a, 0xb0));
    private static readonly IBrush Body = new SolidColorBrush(Color.FromRgb(0xd0, 0xd0, 0xd8));
    private static readonly IBrush Accent = new SolidColorBrush(Color.FromRgb(0xa0, 0xc0, 0xe0));

    private const double MinScale = 0.6;
    private const double MaxScale = 3.0;
    private const double ZoomStep = 1.15;

    private readonly StackPanel _root;
    private GuideDocument? _doc;
    private double _fontScale = 1.0;

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

        KeyDown += OnKeyDown;
        PointerWheelChanged += OnPointerWheel;
    }

    /// <summary>Store and render a new document (used on open and on fractal/formula
    /// switch while open). Rendering honors the current zoom level.</summary>
    public void Populate(GuideDocument doc)
    {
        _doc = doc;
        Render();
    }

    // ----------------------------------------------------------------- zoom
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { Close(); return; }
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;

        switch (e.Key)
        {
            case Key.OemPlus:
            case Key.Add:
                ZoomBy(ZoomStep); e.Handled = true; break;
            case Key.OemMinus:
            case Key.Subtract:
                ZoomBy(1.0 / ZoomStep); e.Handled = true; break;
            case Key.D0:
            case Key.NumPad0:
                _fontScale = 1.0; Render(); e.Handled = true; break;
        }
    }

    private void OnPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control)) return;
        ZoomBy(e.Delta.Y > 0 ? ZoomStep : 1.0 / ZoomStep);
        e.Handled = true;   // zoom instead of scroll while Ctrl is held
    }

    private void ZoomBy(double factor)
    {
        _fontScale = Math.Clamp(_fontScale * factor, MinScale, MaxScale);
        Render();
    }

    /// <summary>Scale a base font size by the current zoom level.</summary>
    private double S(double baseSize) => baseSize * _fontScale;

    // --------------------------------------------------------------- render
    private void Render()
    {
        _root.Children.Clear();
        if (_doc is null) return;

        Title = $"Guide: {_doc.Title}";

        _root.Children.Add(new SelectableTextBlock
        {
            Text = _doc.Title,
            Foreground = Body,
            FontSize = S(20),
            FontWeight = FontWeight.Bold,
            Margin = new Thickness(0, 0, 0, 8),
        });

        foreach (var block in _doc.Blocks)
        {
            switch (block)
            {
                case GuideBlock.Heading h:
                    _root.Children.Add(new SelectableTextBlock
                    {
                        Text = h.Text.ToUpperInvariant(),
                        Foreground = Accent,
                        FontSize = S(13),
                        FontWeight = FontWeight.SemiBold,
                        Margin = new Thickness(0, 14, 0, 2),
                    });
                    break;

                case GuideBlock.Paragraph p:
                    _root.Children.Add(new SelectableTextBlock
                    {
                        Text = p.Text,
                        Foreground = Body,
                        FontSize = S(13),
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 0, 0, 4),
                    });
                    break;

                case GuideBlock.SettingGroupHeading g:
                    _root.Children.Add(new SelectableTextBlock
                    {
                        Text = g.Group.ToUpperInvariant(),
                        Foreground = Dim,
                        FontSize = S(11),
                        FontWeight = FontWeight.SemiBold,
                        Margin = new Thickness(0, 10, 0, 2),
                    });
                    break;

                case GuideBlock.SettingDefinition d:
                    _root.Children.Add(BuildDefinition(d));
                    break;

                case GuideBlock.GlossaryItem gi:
                    _root.Children.Add(BuildGlossaryItem(gi));
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
            FontSize = S(12),
            FontWeight = FontWeight.Bold,
        });
        header.Children.Add(new SelectableTextBlock
        {
            Text = d.Range,
            Foreground = Dim,
            FontSize = S(11),
            FontFamily = new FontFamily("monospace"),
            VerticalAlignment = VerticalAlignment.Center,
        });
        box.Children.Add(header);

        if (!string.IsNullOrEmpty(d.Note))
            box.Children.Add(new SelectableTextBlock
            {
                Text = d.Note,
                Foreground = Body,
                FontSize = S(12),
                TextWrapping = TextWrapping.Wrap,
            });

        return box;
    }

    private Control BuildGlossaryItem(GuideBlock.GlossaryItem g)
    {
        var box = new StackPanel { Spacing = 1, Margin = new Thickness(0, 2, 0, 4) };
        box.Children.Add(new SelectableTextBlock
        {
            Text = g.Term,
            Foreground = Body,
            FontSize = S(12),
            FontWeight = FontWeight.Bold,
        });
        box.Children.Add(new SelectableTextBlock
        {
            Text = g.Definition,
            Foreground = Dim,
            FontSize = S(12),
            TextWrapping = TextWrapping.Wrap,
        });
        return box;
    }
}
