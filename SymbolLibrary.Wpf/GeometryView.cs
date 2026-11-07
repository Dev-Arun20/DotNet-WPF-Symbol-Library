using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SymbolLibrary.Core;

namespace SymbolLibrary.Wpf;

// Shared vector painter for thumbnails and the drawing area (no CAD viewer control).
public class GeometryView : FrameworkElement
{
    public GeometryDocument? Geometry { get => (GeometryDocument?)GetValue(GeometryProperty); set => SetValue(GeometryProperty, value); }
    public static readonly DependencyProperty GeometryProperty = DependencyProperty.Register(nameof(Geometry), typeof(GeometryDocument), typeof(GeometryView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (_, __) => { }));
    public bool Interactive { get; set; }
    public Brush Ink { get; set; } = Brushes.LightSlateGray;
    private readonly List<(GeometryDocument Geometry, Pt At)> placed = new();
    private double scale = 1, offsetX, offsetY;
    private bool fitted;
    public int PlacedCount => placed.Count;
    public event Action<int>? PlacementCountChanged;

    public GeometryView()
    {
        ClipToBounds = true;
        SizeChanged += (_, _) => { if (Interactive && Geometry is not null && !fitted) Fit(); else if (!Interactive) InvalidateVisual(); };
        MouseWheel += OnWheel;
    }
    public void Open(GeometryDocument drawing)
    {
        Geometry = drawing;
        placed.Clear();
        PlacementCountChanged?.Invoke(0);
        fitted = false;
        Fit();
    }
    public void Fit()
    {
        if (Geometry is null || ActualWidth < 2 || ActualHeight < 2) return;
        var b = Geometry.Bounds;
        scale = Math.Min((ActualWidth - 72) / Math.Max(1, b.Width), (ActualHeight - 72) / Math.Max(1, b.Height));
        scale = Math.Max(0.00001, scale);
        offsetX = ActualWidth/2 - b.Center.X*scale;
        offsetY = ActualHeight/2 + b.Center.Y*scale;
        fitted = true;
        InvalidateVisual();
    }
    public Pt ScreenToWorld(Point p) => new((p.X-offsetX)/scale, (offsetY-p.Y)/scale);
    public void AddAt(GeometryDocument symbol, Pt target)
    {
        // Anchor the symbol centre at the drop position, preserving all original DXF mm.
        placed.Add((symbol, target));
        PlacementCountChanged?.Invoke(placed.Count);
        InvalidateVisual();
    }
    private void OnWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Interactive || Geometry is null) return;
        var mouse = e.GetPosition(this);
        var world = ScreenToWorld(mouse);
        scale = Math.Clamp(scale * Math.Pow(1.12, e.Delta / 120.0), .00001, 500);
        offsetX = mouse.X - world.X*scale;
        offsetY = mouse.Y + world.Y*scale;
        InvalidateVisual();
        e.Handled = true;
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(0,0,ActualWidth,ActualHeight));
        if (Geometry is null) return;
        if (!Interactive)
        {
            var b=Geometry.Bounds;
            double s = Math.Min((ActualWidth-18)/Math.Max(1,b.Width), (ActualHeight-18)/Math.Max(1,b.Height));
            s = Math.Max(.00001,s);
            Paint(dc,Geometry,s,ActualWidth/2-b.Center.X*s,ActualHeight/2+b.Center.Y*s,0,0,Ink);
            return;
        }
        Paint(dc, Geometry, scale, offsetX,offsetY,0,0,Ink);
        foreach (var item in placed)
        {
            var center=item.Geometry.Bounds.Center;
            Paint(dc,item.Geometry,scale,offsetX,offsetY,item.At.X-center.X,item.At.Y-center.Y,Brushes.DeepSkyBlue);
        }
    }
    private static void Paint(DrawingContext dc, GeometryDocument geometry, double scale, double ox, double oy, double dx, double dy, Brush ink)
    {
        var pen = new Pen(ink, 1.15); pen.Freeze();
        Point Project(Pt p) => new(ox+(p.X+dx)*scale, oy-(p.Y+dy)*scale);
        foreach (var primitive in geometry.Primitives)
        {
            if (primitive is Stroke stroke)
            {
                for (int i=1; i<stroke.Points.Count; i++) dc.DrawLine(pen,Project(stroke.Points[i-1]),Project(stroke.Points[i]));
            }
            else if (primitive is Label t && t.Value.Length > 0)
            {
                var p=Project(t.Position);
                var font = new FormattedText(t.Value,CultureInfo.InvariantCulture,FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"), Math.Clamp(t.Height*scale, 3, 64),ink,1.0);
                dc.PushTransform(new RotateTransform(-t.Angle,p.X,p.Y));
                dc.DrawText(font,new Point(p.X,p.Y-font.Height));
                dc.Pop();
            }
        }
    }
}
