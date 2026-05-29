using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Parsec.App;

/// <summary>
/// The bottom keyframe strip: a row of fixed cells. Left-click selects a cell;
/// right-click clears it (slot 0 is protected). Cells show three states: empty
/// (outline), set (filled), and selected (accent border). Pure UI -- it raises
/// events; the host wires them to the <see cref="Timeline"/> and playback.
/// </summary>
public sealed class KeyframeBank : UserControl
{
    public event Action<int>? CellSelected;
    public event Action<int>? CellCleared;

    private readonly Border[] _cells = new Border[Timeline.SlotCount];
    private int _selected;

    private static readonly IBrush EmptyFill = new SolidColorBrush(Color.FromRgb(0x2a, 0x2a, 0x30));
    private static readonly IBrush SetFill = new SolidColorBrush(Color.FromRgb(0x3a, 0x6e, 0xa5));
    private static readonly IBrush EmptyStroke = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x4c));
    private static readonly IBrush SelectedStroke = new SolidColorBrush(Color.FromRgb(0xe0, 0xc0, 0x60));

    public KeyframeBank()
    {
        Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1a, 0x1e));
        Height = 52;

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            Margin = new Avalonia.Thickness(8, 8, 8, 8),
            VerticalAlignment = VerticalAlignment.Center,
        };

        for (int i = 0; i < Timeline.SlotCount; i++)
        {
            int idx = i;
            var cell = new Border
            {
                Width = 22,
                Height = 32,
                CornerRadius = new Avalonia.CornerRadius(3),
                Background = EmptyFill,
                BorderBrush = EmptyStroke,
                BorderThickness = new Avalonia.Thickness(1),
                Child = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontSize = 9,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x90)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                },
            };
            cell.PointerPressed += (s, e) =>
            {
                var pt = e.GetCurrentPoint(cell);
                if (pt.Properties.IsRightButtonPressed)
                    CellCleared?.Invoke(idx);
                else
                    CellSelected?.Invoke(idx);
            };
            _cells[i] = cell;
            row.Children.Add(cell);
        }

        Content = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = row,
        };
    }

    /// <summary>Refresh all cells from timeline state.</summary>
    public void Refresh(Timeline timeline)
    {
        _selected = timeline.SelectedIndex;
        for (int i = 0; i < Timeline.SlotCount; i++)
        {
            bool set = timeline.IsSet(i);
            bool sel = i == _selected;
            _cells[i].Background = set ? SetFill : EmptyFill;
            _cells[i].BorderBrush = sel ? SelectedStroke : EmptyStroke;
            _cells[i].BorderThickness = new Avalonia.Thickness(sel ? 2 : 1);
        }
    }

    /// <summary>Enable/disable the whole bank (disabled for the attractor).</summary>
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        Opacity = enabled ? 1.0 : 0.4;
    }
}
