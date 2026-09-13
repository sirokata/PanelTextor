using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
namespace PanelTextor;

// HarfBuzzSharp exposes variable-axis shaping, but not hb_font_draw_glyph.
// Use the same bundled native HarfBuzz to extract the varied CFF2/TrueType path.
internal static class HarfBuzzOutline
{
 private const string Library = "libHarfBuzzSharp";
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void PointCallback(IntPtr f, IntPtr data, IntPtr state, float x, float y, IntPtr user);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void QuadCallback(IntPtr f, IntPtr data, IntPtr state, float cx, float cy, float x, float y, IntPtr user);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void CubicCallback(IntPtr f, IntPtr data, IntPtr state, float ax, float ay, float bx, float by, float x, float y, IntPtr user);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void CloseCallback(IntPtr f, IntPtr data, IntPtr state, IntPtr user);
 [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern IntPtr hb_draw_funcs_create();
 [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void hb_draw_funcs_make_immutable(IntPtr f);
 [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void hb_draw_funcs_set_move_to_func(IntPtr f, PointCallback cb, IntPtr user, IntPtr destroy);
 [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void hb_draw_funcs_set_line_to_func(IntPtr f, PointCallback cb, IntPtr user, IntPtr destroy);
 [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void hb_draw_funcs_set_quadratic_to_func(IntPtr f, QuadCallback cb, IntPtr user, IntPtr destroy);
 [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void hb_draw_funcs_set_cubic_to_func(IntPtr f, CubicCallback cb, IntPtr user, IntPtr destroy);
 [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void hb_draw_funcs_set_close_path_func(IntPtr f, CloseCallback cb, IntPtr user, IntPtr destroy);
 [DllImport(Library, CallingConvention = CallingConvention.Cdecl)] private static extern void hb_font_draw_glyph(IntPtr font, uint glyph, IntPtr funcs, IntPtr data);
 private sealed class Sink(StreamGeometryContext context, double scale)
 {
  public Exception? Error;
  public Point P(float x, float y) => new(x * scale, -y * scale);
  public void Apply(Action<StreamGeometryContext> action) { if (Error is not null) return; try { action(context); } catch (Exception e) { Error = e; } }
 }
 private static Sink Get(IntPtr ptr) => (Sink)GCHandle.FromIntPtr(ptr).Target!;
 // Root callback delegates for the complete lifetime of the native funcs object.
 private static readonly PointCallback move = (_, d, _, x, y, _) => { var s = Get(d); s.Apply(c => c.BeginFigure(s.P(x, y), true, true)); };
 private static readonly PointCallback line = (_, d, _, x, y, _) => { var s = Get(d); s.Apply(c => c.LineTo(s.P(x, y), true, false)); };
 private static readonly QuadCallback quad = (_, d, _, cx, cy, x, y, _) => { var s = Get(d); s.Apply(c => c.QuadraticBezierTo(s.P(cx, cy), s.P(x, y), true, false)); };
 private static readonly CubicCallback cubic = (_, d, _, ax, ay, bx, by, x, y, _) => { var s = Get(d); s.Apply(c => c.BezierTo(s.P(ax, ay), s.P(bx, by), s.P(x, y), true, false)); };
 private static readonly CloseCallback close = (_, _, _, _) => { }; // BeginFigure closes each contour.
 private static readonly IntPtr funcs = Create();
 private static IntPtr Create()
 {
  var f = hb_draw_funcs_create();
  hb_draw_funcs_set_move_to_func(f, move, IntPtr.Zero, IntPtr.Zero);
  hb_draw_funcs_set_line_to_func(f, line, IntPtr.Zero, IntPtr.Zero);
  hb_draw_funcs_set_quadratic_to_func(f, quad, IntPtr.Zero, IntPtr.Zero);
  hb_draw_funcs_set_cubic_to_func(f, cubic, IntPtr.Zero, IntPtr.Zero);
  hb_draw_funcs_set_close_path_func(f, close, IntPtr.Zero, IntPtr.Zero);
  hb_draw_funcs_make_immutable(f); return f;
 }
 public static Geometry Build(IntPtr font, ushort glyph, double unitsPerEm)
 {
  var geometry = new StreamGeometry { FillRule = FillRule.Nonzero };
  using (var context = geometry.Open())
  {
   var sink = new Sink(context, 1 / unitsPerEm);
   var handle = GCHandle.Alloc(sink);
   try { hb_font_draw_glyph(font, glyph, funcs, GCHandle.ToIntPtr(handle)); if (sink.Error is not null) throw new InvalidOperationException("字形の輪郭を取得できません。", sink.Error); }
   finally { handle.Free(); }
  }
  geometry.Freeze(); return geometry;
 }
}

