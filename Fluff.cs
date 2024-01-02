#if BFLAT
using System.Runtime.InteropServices;
class Math
{
    [DllImport("*", EntryPoint = "sin", CallingConvention = CallingConvention.Cdecl)]
    public static extern double Sin(double x);

    [DllImport("*", EntryPoint = "cos", CallingConvention = CallingConvention.Cdecl)]
    public static extern double Cos(double x);

    [DllImport("*", EntryPoint = "fabs", CallingConvention = CallingConvention.Cdecl)]
    public static extern double Abs(double x);
}
#endif
