using System;
using System.Runtime.InteropServices;

internal unsafe class Win32
{
    public const int WS_VISIBLE = 0x10000000;
    public const int WS_CAPTION = 0x00C00000;
    public const int WS_CLIPSIBLINGS = 0x04000000;
    public const int WS_CLIPCHILDREN = 0x02000000;

    public const int WS_EX_APPWINDOW = 0x00040000;
    public const int WS_EX_WINDOWEDGE = 0x00000100;

    public const int PM_REMOVE = 0x0001;

    public const int DIB_RGB_COLORS = 0;

    public const uint SRCCOPY = (uint)(0x00CC0020);

    public const int WM_KEYDOWN = 0x0100;
    public const int WM_KEYUP = 0x0101;

    public const int VK_ESCAPE = 0x1B;

    public enum BOOL : int { FALSE = 0 }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RGBQUAD
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
        public byte rgbReserved;
    }

    public enum BI
    {
        RGB = 0,
        RLE8 = 1,
        RLE4 = 2,
        BITFIELDS = 3,
        JPEG = 4,
        PNG = 5,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public BI biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public RGBQUAD bmiColors;
    }

    [DllImport("user32")]
    public static extern IntPtr CreateWindowExA(
        int dwExStyle,
        byte* lpClassName,
        byte* lpWindowName,
        int dwStyle,
        int X,
        int Y,
        int nWidth,
        int nHeight,
        IntPtr hWndParent,
        IntPtr hMenu,
        IntPtr hInst,
        IntPtr lParam);

    [DllImport("user32")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("kernel32")]
    public static extern uint GetTickCount();

    [DllImport("user32")]
    public static extern BOOL PeekMessageA(MSG* lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

    [DllImport("user32")]
    public static extern IntPtr DispatchMessageA(MSG* msg);

    [DllImport("gdi32")]
    public static extern int StretchDIBits(IntPtr hdc, int xDest, int yDest, int DestWidth, int DestHeight, int xSrc, int ySrc, int SrcWidth, int SrcHeight, void* lpBits, BITMAPINFO* lpbmi, uint iUsage, uint rop);

    [DllImport("user32")]
    public static extern short GetAsyncKeyState(int vKey);
}
