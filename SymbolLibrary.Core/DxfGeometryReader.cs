using netDxf;
using netDxf.Entities;
using netDxf.Blocks;
using DxfText = netDxf.Entities.Text;
using DxfLine = netDxf.Entities.Line;
using DxfArc = netDxf.Entities.Arc;

namespace SymbolLibrary.Core;

public sealed class DxfGeometryReader
{
    public GeometryDocument Read(string path, string? blockName = null)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("DXF not found", path);
        var doc = DxfDocument.Load(path) ?? throw new InvalidDataException("Cannot read DXF: " + path);
        var result = new GeometryDocument();
        if (blockName is null) DrawEntities(doc.Entities.All, Affine.Identity, result, 0, new HashSet<string>());
        else
        {
            var block = doc.Blocks[blockName] ?? throw new InvalidDataException("Block not found: " + blockName);
            DrawEntities(block.Entities, Affine.Translate(-block.Origin.X, -block.Origin.Y), result, 0, new HashSet<string>());
        }
        return result;
    }

    private static Pt P(netDxf.Vector2 p) => new(p.X, p.Y);
    private static Pt P(netDxf.Vector3 p) => new(p.X, p.Y);
    private static void DrawEntities(IEnumerable<EntityObject> entities, Affine transform, GeometryDocument into, int depth, HashSet<string> stack)
    {
        if (depth > 20) return; // malformed circular block nesting guard
        foreach (var entity in entities)
        {
            switch (entity)
            {
                case DxfLine line:
                    into.Primitives.Add(new Stroke(new[] {transform.Apply(P(line.StartPoint)), transform.Apply(P(line.EndPoint))}));
                    break;
                case Circle circle:
                    AddArc(into, transform, P(circle.Center), circle.Radius, 0, 360);
                    break;
                case DxfArc arc:
                    AddArc(into, transform, P(arc.Center), arc.Radius, arc.StartAngle, arc.EndAngle);
                    break;
                case Polyline2D poly:
                    var verts = poly.Vertexes;
                    int n = verts.Count;
                    int segments = poly.IsClosed ? n : n - 1;
                    for (int i = 0; i < segments; i++)
                    {
                        var a = verts[i]; var b = verts[(i+1)%n];
                        var p = P(a.Position); var q = P(b.Position);
                        if (Math.Abs(a.Bulge) < 1e-10)
                            into.Primitives.Add(new Stroke(new[] {transform.Apply(p), transform.Apply(q)}));
                        else AddBulge(into, transform, p, q, a.Bulge);
                    }
                    break;
                case DxfText t:
                    var point = transform.Apply(P(t.Position));
                    var top = transform.Apply(P(t.Position) + new Pt(0, Math.Max(.001, t.Height)));
                    double h = Math.Sqrt(Math.Pow(top.X-point.X, 2) + Math.Pow(top.Y-point.Y, 2));
                    var axis = transform.Apply(P(t.Position) + new Pt(Math.Cos(t.Rotation*Math.PI/180),Math.Sin(t.Rotation*Math.PI/180))) - point;
                    into.Primitives.Add(new Label(t.Value, point, h, Math.Atan2(axis.Y, axis.X)*180/Math.PI));
                    break;
                case Insert insert when insert.Block is not null:
                    var name = insert.Block.Name;
                    if (!stack.Add(name)) break;
                    try
                    {
                        // Correct order: insert position + rotate * scale * (point - block origin).
                        var tform = transform * Affine.Translate(insert.Position.X, insert.Position.Y)
                            * Affine.Rotate(insert.Rotation) * Affine.Scale(insert.Scale.X, insert.Scale.Y)
                            * Affine.Translate(-insert.Block.Origin.X, -insert.Block.Origin.Y);
                        DrawEntities(insert.Block.Entities, tform, into, depth+1, stack);
                    }
                    finally { stack.Remove(name); }
                    break;
            }
        }
    }
    private static void AddArc(GeometryDocument result, Affine t, Pt center, double radius, double start, double end)
    {
        if (radius <= 0) return;
        double sweep = (end - start + 360) % 360;
        if (Math.Abs(end-start) >= 359.999) sweep = 360;
        if (sweep < 1e-8) return;
        int steps = Math.Max(2, (int)Math.Ceiling(sweep / 4));
        var pts = new Pt[steps+1];
        for (int i=0;i<=steps;i++)
        {
            double theta=(start+sweep*i/steps)*Math.PI/180;
            pts[i]=t.Apply(new Pt(center.X+radius*Math.Cos(theta),center.Y+radius*Math.Sin(theta)));
        }
        result.Primitives.Add(new Stroke(pts));
    }
    private static void AddBulge(GeometryDocument result, Affine t, Pt a, Pt b, double bulge)
    {
        double dx=b.X-a.X, dy=b.Y-a.Y, chord=Math.Sqrt(dx*dx+dy*dy);
        if (chord < 1e-10) return;
        double sweep=4*Math.Atan(bulge);
        double midpointX=(a.X+b.X)/2, midpointY=(a.Y+b.Y)/2;
        double offset=chord*(1-bulge*bulge)/(4*bulge);
        var center=new Pt(midpointX-dy/chord*offset, midpointY+dx/chord*offset);
        double start=Math.Atan2(a.Y-center.Y,a.X-center.X);
        double radius=Math.Sqrt(Math.Pow(a.X-center.X,2)+Math.Pow(a.Y-center.Y,2));
        int steps=Math.Max(2,(int)Math.Ceiling(Math.Abs(sweep)*180/Math.PI/4));
        var pts=new Pt[steps+1];
        for(int i=0;i<=steps;i++)
        {
            double theta=start+sweep*i/steps;
            pts[i]=t.Apply(new Pt(center.X+radius*Math.Cos(theta),center.Y+radius*Math.Sin(theta)));
        }
        result.Primitives.Add(new Stroke(pts));
    }
}
