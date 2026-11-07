namespace SymbolLibrary.Core;

public readonly record struct Pt(double X, double Y)
{
    public static Pt operator +(Pt a, Pt b) => new(a.X + b.X, a.Y + b.Y);
    public static Pt operator -(Pt a, Pt b) => new(a.X - b.X, a.Y - b.Y);
}

// x' = A*x + C*y + Tx; y' = B*x + D*y + Ty
public readonly record struct Affine(double A, double B, double C, double D, double Tx, double Ty)
{
    public static Affine Identity => new(1, 0, 0, 1, 0, 0);
    public static Affine Translate(double x, double y) => new(1, 0, 0, 1, x, y);
    public static Affine Scale(double x, double y) => new(x, 0, 0, y, 0, 0);
    public static Affine Rotate(double degrees)
    {
        double r = degrees * Math.PI / 180, c = Math.Cos(r), s = Math.Sin(r);
        return new(c, s, -s, c, 0, 0);
    }
    public Pt Apply(Pt p) => new(A*p.X + C*p.Y + Tx, B*p.X + D*p.Y + Ty);
    // Compose: left * right applies right first.
    public static Affine operator *(Affine l, Affine r) => new(
        l.A*r.A+l.C*r.B, l.B*r.A+l.D*r.B,
        l.A*r.C+l.C*r.D, l.B*r.C+l.D*r.D,
        l.A*r.Tx+l.C*r.Ty+l.Tx, l.B*r.Tx+l.D*r.Ty+l.Ty);
}

public readonly record struct Bounds(double MinX, double MinY, double MaxX, double MaxY)
{
    public double Width => Math.Max(0, MaxX - MinX);
    public double Height => Math.Max(0, MaxY - MinY);
    public Pt Center => new((MinX + MaxX)/2, (MinY + MaxY)/2);
}

public abstract record Primitive;
public sealed record Stroke(IReadOnlyList<Pt> Points) : Primitive;
public sealed record Label(string Value, Pt Position, double Height, double Angle) : Primitive;

public sealed class GeometryDocument
{
    public List<Primitive> Primitives { get; } = new();
    public Bounds Bounds
    {
        get
        {
            var points = Primitives.SelectMany(p => p switch
            {
                Stroke s => s.Points,
                Label t => new[] { t.Position, new Pt(t.Position.X + t.Height * Math.Max(1, t.Value.Length) * .65, t.Position.Y + t.Height) },
                _ => Array.Empty<Pt>()
            }).ToArray();
            if (points.Length == 0) return new Bounds(0, 0, 1, 1);
            return new Bounds(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X), points.Max(p => p.Y));
        }
    }
    public void AddTransformed(GeometryDocument source, Affine transform)
    {
        foreach (var p in source.Primitives)
        {
            if (p is Stroke s) Primitives.Add(new Stroke(s.Points.Select(transform.Apply).ToArray()));
            if (p is Label t)
            {
                var start = transform.Apply(t.Position);
                var direction = transform.Apply(t.Position + new Pt(Math.Cos(t.Angle*Math.PI/180)*t.Height, Math.Sin(t.Angle*Math.PI/180)*t.Height));
                var delta = direction - start;
                Primitives.Add(new Label(t.Value, start, Math.Sqrt(delta.X*delta.X+delta.Y*delta.Y), Math.Atan2(delta.Y, delta.X)*180/Math.PI));
            }
        }
    }
}
