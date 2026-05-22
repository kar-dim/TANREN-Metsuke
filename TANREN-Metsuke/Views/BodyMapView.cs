using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using TANREN_Metsuke.Models;

namespace TANREN_Metsuke.Views;

// shared interactive body map: builds the muscle group shapes over body.png, handles hover highlight
// and the sliding detail panel. Subclasses supply the coloring, hover text and detail panel contents that differ between the views
public abstract class BodyMapView : UserControl
{
    protected readonly Dictionary<MuscleGroup, List<Path>> AllPaths = [];
    protected readonly Dictionary<MuscleGroup, SolidColorBrush> FillBrushes = [];

    protected const byte NormalAlpha = 115;
    protected virtual byte HoverAlpha => 175;

    private Viewbox bodyViewbox = null!;
    private Border detailPanel = null!;
    private Canvas overlayCanvas = null!;
    private TextBlock hoverInfo = null!;

    private bool detailOpen;
    protected MuscleGroup? OpenMuscle { get; private set; }

    // hooks for the parts that differ between subclasses
    protected abstract Color ColorForGroup(MuscleGroup group);
    protected abstract string HoverTextFor(MuscleGroup group);
    protected abstract string DefaultHoverText { get; }
    protected abstract bool CanOpenDetail(MuscleGroup group);
    protected abstract void PopulateDetail(MuscleGroup group);
    protected virtual void OnViewModelChanged() { }

    // called by the subclass constructor (after InitializeComponent)
    protected void AttachBodyMap()
    {
        bodyViewbox = this.FindControl<Viewbox>("BodyViewbox")!;
        detailPanel = this.FindControl<Border>("DetailPanel")!;
        overlayCanvas = this.FindControl<Canvas>("OverlayCanvas")!;
        hoverInfo = this.FindControl<TextBlock>("HoverInfo")!;
        this.FindControl<Button>("CloseDetailBtn")!.Click += (_, _) => CloseDetail();

        var ease = new CubicEaseInOut();
        bodyViewbox.Transitions =
        [
            new ThicknessTransition
            {
                Property = Layoutable.MarginProperty,
                Duration = TimeSpan.FromMilliseconds(350),
                Easing = ease
            }
        ];
        detailPanel.Transitions =
        [
            new DoubleTransition
            {
                Property = Layoutable.WidthProperty,
                Duration = TimeSpan.FromMilliseconds(350),
                Easing = ease
            }
        ];

        // resize the open panel (without animating)
        this.GetObservable(BoundsProperty).Subscribe(bounds =>
        {
            if (!detailOpen)
                return;
            var half = Math.Max(0, (bounds.Width - 40) / 2); // use Max for not crashing when the window is too narrow
            var dt = detailPanel.Transitions;
            var bt = bodyViewbox.Transitions;
            detailPanel.Transitions = null;
            bodyViewbox.Transitions = null;
            detailPanel.Width = half;
            bodyViewbox.Margin = new Thickness(0, 0, half, 0);
            detailPanel.Transitions = dt;
            bodyViewbox.Transitions = bt;
        });
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext == null)
            return;

        AllPaths.Clear();
        FillBrushes.Clear();
        overlayCanvas.Children.Clear();

        AddLabels();
        foreach (var def in BodyMapHelper.AllRegions())
            BuildRegion(def);
        ApplyColors();

        OnViewModelChanged();
        RefreshOpenDetail();

        // Add transitions after initial colors are set, so startup doesn't animate
        foreach (var brush in FillBrushes.Values)
            brush.Transitions =
            [
                new ColorTransition
                {
                    Property = SolidColorBrush.ColorProperty,
                    Duration = TimeSpan.FromMilliseconds(160)
                }
            ];
    }

    protected void ApplyColors()
    {
        foreach (var (group, brush) in FillBrushes)
        {
            var c = ColorForGroup(group);
            brush.Color = Color.FromArgb(NormalAlpha, c.R, c.G, c.B);
        }
    }

    protected void RefreshOpenDetail()
    {
        if (detailOpen && OpenMuscle.HasValue)
            PopulateDetail(OpenMuscle.Value);
    }

    private void CloseDetail()
    {
        detailOpen = false;
        OpenMuscle = null;
        detailPanel.Width = 0;
        bodyViewbox.Margin = new Thickness(0);
    }

    private void AddLabels()
    {
        AddLabel("FRONT", 120, 55);
        AddLabel("BACK", 775, 55);
    }

    private void AddLabel(string text, double x, double y)
    {
        var tb = new TextBlock
        {
            Text = text,
            FontSize = 22,
            FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(BodyMapHelper.BodyLabelGray),
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(tb, x);
        Canvas.SetTop(tb, y);
        overlayCanvas.Children.Add(tb);
    }

    private void BuildRegion(RegionDef d)
    {
        if (!FillBrushes.ContainsKey(d.Group))
            FillBrushes[d.Group] = new SolidColorBrush(Color.FromArgb(NormalAlpha, BodyMapHelper.MuscleInactive.R, BodyMapHelper.MuscleInactive.G, BodyMapHelper.MuscleInactive.B));

        var path = new Path
        {
            Data = BodyMapHelper.MakeGeometry(d.Shape, d.W, d.H),
            Fill = FillBrushes[d.Group],
            Cursor = new Cursor(StandardCursorType.Hand),
            Tag = d.Group,
            Transitions =
            [
                new DoubleTransition
                {
                    Property = Shape.StrokeThicknessProperty,
                    Duration = TimeSpan.FromMilliseconds(160)
                }
            ]
        };

        Canvas.SetLeft(path, d.X);
        Canvas.SetTop(path, d.Y);

        if (d.Mirror || d.Rotation != 0)
        {
            var tg = new TransformGroup();
            if (d.Rotation != 0)
                tg.Children.Add(new RotateTransform(d.Rotation));
            if (d.Mirror)
                tg.Children.Add(new ScaleTransform(-1, 1));
            path.RenderTransform = tg;
            path.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        }

        path.PointerEntered += OnEnter;
        path.PointerExited += OnLeave;
        path.PointerPressed += OnClick;

        overlayCanvas.Children.Add(path);

        if (!AllPaths.ContainsKey(d.Group))
            AllPaths[d.Group] = [];
        AllPaths[d.Group].Add(path);
    }

    private void SetGroupHighlight(MuscleGroup group, bool highlighted)
    {
        var c = ColorForGroup(group);
        FillBrushes[group].Color = Color.FromArgb(highlighted ? HoverAlpha : NormalAlpha, c.R, c.G, c.B);
        foreach (var sib in AllPaths.GetValueOrDefault(group) ?? [])
        {
            sib.StrokeThickness = highlighted ? 2 : 0;
            sib.Stroke = highlighted ? Brushes.White : null;
        }
    }

    private void OnEnter(object? sender, PointerEventArgs e)
    {
        if (sender is not Path p)
            return;
        var group = (MuscleGroup)p.Tag!;
        SetGroupHighlight(group, true);
        hoverInfo.Text = HoverTextFor(group);
    }

    private void OnLeave(object? sender, PointerEventArgs e)
    {
        if (sender is not Path p)
            return;
        var group = (MuscleGroup)p.Tag!;
        SetGroupHighlight(group, false);
        hoverInfo.Text = DefaultHoverText;
    }

    private void OnClick(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Path p)
            return;
        var group = (MuscleGroup)p.Tag!;
        if (!CanOpenDetail(group))
            return;

        OpenMuscle = group;
        PopulateDetail(group);

        detailOpen = true;
        var half = Math.Max(0, (Bounds.Width - 40) / 2); // we use Max, it was crashing when the window was too narrow
        detailPanel.Width = half;
        bodyViewbox.Margin = new Thickness(0, 0, half, 0);
    }
}
