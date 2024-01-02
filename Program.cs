using System;
using System.Runtime.CompilerServices;

using static Win32;

unsafe class Program
{
    const int Width = 640;
    const int Height = 480;

    const int MapWidth = 24;
    const int MapHeight = 24;

    static ReadOnlySpan<byte> WorldMap => [
        1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
        1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,0,0,0,0,0,2,2,2,2,2,0,0,0,0,3,0,3,0,3,0,0,0,1,
        1,0,0,0,0,0,2,0,0,0,2,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,0,0,0,0,0,2,0,0,0,2,0,0,0,0,3,0,0,0,3,0,0,0,1,
        1,0,0,0,0,0,2,0,0,0,2,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,0,0,0,0,0,2,2,0,2,2,0,0,0,0,3,0,3,0,3,0,0,0,1,
        1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,4,4,4,4,4,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,4,0,4,0,0,0,0,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,4,0,0,0,0,5,0,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,4,0,4,0,0,0,0,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,4,0,4,4,4,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,4,4,4,4,4,4,4,4,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,
        1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1];

    private static void RenderEffect(uint tick, byte* framebuf)
    {
        new Span<int>(framebuf, Width * Height / 2).Fill(0x110000);
        new Span<int>((int*)framebuf + Width * Height / 2, Width * Height / 2).Fill(0x333333);

        for (int x = 0; x < Width; x++)
        {
            //calculate ray position and direction
            double cameraX = 2 * x / (double)Width - 1; //x-coordinate in camera space
            double rayDirX = dirX + planeX * cameraX;
            double rayDirY = dirY + planeY * cameraX;
            //which box of the map we're in
            int mapX = (int)posX;
            int mapY = (int)posY;

            //length of ray from current position to next x or y-side
            double sideDistX;
            double sideDistY;

            //length of ray from one x or y-side to next x or y-side
            //these are derived as:
            //deltaDistX = sqrt(1 + (rayDirY * rayDirY) / (rayDirX * rayDirX))
            //deltaDistY = sqrt(1 + (rayDirX * rayDirX) / (rayDirY * rayDirY))
            //which can be simplified to abs(|rayDir| / rayDirX) and abs(|rayDir| / rayDirY)
            //where |rayDir| is the length of the vector (rayDirX, rayDirY). Its length,
            //unlike (dirX, dirY) is not 1, however this does not matter, only the
            //ratio between deltaDistX and deltaDistY matters, due to the way the DDA
            //stepping further below works. So the values can be computed as below.
            // Division through zero is prevented, even though technically that's not
            // needed in C++ with IEEE 754 floating point values.
            double deltaDistX = (rayDirX == 0) ? 1e30 : Math.Abs(1 / rayDirX);
            double deltaDistY = (rayDirY == 0) ? 1e30 : Math.Abs(1 / rayDirY);

            double perpWallDist;

            //what direction to step in x or y-direction (either +1 or -1)
            int stepX;
            int stepY;

            int hit = 0; //was there a wall hit?
            int side = 0; //was a NS or a EW wall hit?
                      //calculate step and initial sideDist
            if (rayDirX < 0)
            {
                stepX = -1;
                sideDistX = (posX - mapX) * deltaDistX;
            }
            else
            {
                stepX = 1;
                sideDistX = (mapX + 1.0 - posX) * deltaDistX;
            }
            if (rayDirY < 0)
            {
                stepY = -1;
                sideDistY = (posY - mapY) * deltaDistY;
            }
            else
            {
                stepY = 1;
                sideDistY = (mapY + 1.0 - posY) * deltaDistY;
            }
            //perform DDA
            while (hit == 0)
            {
                //jump to next map square, either in x-direction, or in y-direction
                if (sideDistX < sideDistY)
                {
                    sideDistX += deltaDistX;
                    mapX += stepX;
                    side = 0;
                }
                else
                {
                    sideDistY += deltaDistY;
                    mapY += stepY;
                    side = 1;
                }
                //Check if ray has hit a wall
                if (WorldMap[mapX * MapHeight + mapY] > 0) hit = 1;
            }
            //Calculate distance projected on camera direction. This is the shortest distance from the point where the wall is
            //hit to the camera plane. Euclidean to center camera point would give fisheye effect!
            //This can be computed as (mapX - posX + (1 - stepX) / 2) / rayDirX for side == 0, or same formula with Y
            //for size == 1, but can be simplified to the code below thanks to how sideDist and deltaDist are computed:
            //because they were left scaled to |rayDir|. sideDist is the entire length of the ray above after the multiple
            //steps, but we subtract deltaDist once because one step more into the wall was taken above.
            if (side == 0) perpWallDist = (sideDistX - deltaDistX);
            else perpWallDist = (sideDistY - deltaDistY);

            //Calculate height of line to draw on screen
            int lineHeight = (int)(Height / perpWallDist);

            //calculate lowest and highest pixel to fill in current stripe
            int drawStart = -lineHeight / 2 + Height / 2;
            if (drawStart < 0) drawStart = 0;
            int drawEnd = lineHeight / 2 + Height / 2;
            if (drawEnd >= Height) drawEnd = Height - 1;

            //choose wall color
            int color = WorldMap[mapX * MapHeight + mapY] switch
            {
                1 => 0xFF,
                2 => 0xFF00,
                3 => 0xFF0000,
                4 => 0xFFFFFF,
                _ => 0xFFFF00,
            };

            if (side == 1)
                color &= ~0x808080;

            //draw the pixels of the stripe as a vertical line
            for (int y = drawStart; y < drawEnd; y++)
                *(int*)(framebuf + (x + y * Width) * 4) = color;

            double frameTime = (tick - time) / 1000.0; //frameTime is the time this frame has taken, in seconds

            //speed modifiers
            double moveSpeed = frameTime * 5.0; //the constant value is in squares/second
            double rotSpeed = frameTime * 3.0; //the constant value is in radians/second

            //move forward or backwards if no wall
            if ((keyState & (KeyState.Up | KeyState.Down)) != 0)
            {
                if ((keyState & KeyState.Down) != 0)
                    moveSpeed = -moveSpeed;
                if (WorldMap[(int)(posX + dirX * moveSpeed) * MapHeight + (int)posY] == 0) posX += dirX * moveSpeed;
                if (WorldMap[(int)posX * MapHeight + (int)(posY + dirY * moveSpeed)] == 0) posY += dirY * moveSpeed;
            }
            ////rotate to the right or left
            if ((keyState & (KeyState.Right | KeyState.Left)) != 0)
            {
                if ((keyState & KeyState.Right) != 0)
                    rotSpeed = -rotSpeed;
                //both camera direction and camera plane must be rotated
                double oldDirX = dirX;
                dirX = dirX * Math.Cos(rotSpeed) - dirY * Math.Sin(rotSpeed);
                dirY = oldDirX * Math.Sin(rotSpeed) + dirY * Math.Cos(rotSpeed);
                double oldPlaneX = planeX;
                planeX = planeX * Math.Cos(rotSpeed) - planeY * Math.Sin(rotSpeed);
                planeY = oldPlaneX * Math.Sin(rotSpeed) + planeY * Math.Cos(rotSpeed);
            }

            time = tick;
        }
    }

    static double posX = 22, posY = 12;  //x and y start position
    static double dirX = -1, dirY = 0; //initial direction vector
    static double planeX = 0, planeY = 0.66; //the 2d raycaster version of camera plane
    static KeyState keyState;
    static uint time;

    enum KeyState
    {
        Left = 1,
        Up = 2,
        Right = 4,
        Down = 8,
    }

    class Screen
    {
        internal static ScreenBuffer s_buffer;
    }

    class BitmapInfo
    {
        internal static BITMAPINFO bmi = new BITMAPINFO
        {
            bmiHeader = new BITMAPINFOHEADER
            {
                biSize = (uint)sizeof(BITMAPINFOHEADER),
                biWidth = Width,
                biHeight = -Height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = BI.RGB,
                biSizeImage = 0,
                biXPelsPerMeter = 0,
                biYPelsPerMeter = 0,
                biClrUsed = 0,
                biClrImportant = 0,
            },
            bmiColors = default
        };
    }

    static void Main()
    {
        long className = 'e' | 'd' << 8 | 'i' << 16 | 't' << 24;
        //long className = 's' | 't' << 8 | 'a' << 16 | 't' << 24 | 'i' << 32 | 'c' << 40;

        IntPtr hwnd = CreateWindowExA(WS_EX_APPWINDOW | WS_EX_WINDOWEDGE, (byte*)&className, null,
                                WS_VISIBLE | WS_CAPTION | WS_CLIPSIBLINGS | WS_CLIPCHILDREN,
                                0, 0, Width, Height, 0, 0, 0, 0);

        IntPtr hdc = GetDC(hwnd);

        bool done = false;
        while (!done)
        {
            MSG msg;
            while (PeekMessageA(&msg, 0, 0, 0, PM_REMOVE) != BOOL.FALSE)
            {
                if (msg.message == WM_KEYDOWN)
                    keyState |= AsKeyState(msg.wParam);
                if (msg.message == WM_KEYUP)
                    keyState &= ~AsKeyState(msg.wParam);

                done |= GetAsyncKeyState(VK_ESCAPE) != 0;

                DispatchMessageA(&msg);

                static KeyState AsKeyState(nint lParam) => (KeyState)(lParam >= 0x25 && lParam <= 0x28 ? 1 << (int)(lParam - 0x25) : 0);
            }

            fixed (BITMAPINFO* pBmi = &BitmapInfo.bmi)
            fixed (ScreenBuffer* pBuffer = &Screen.s_buffer)
            {
                RenderEffect(GetTickCount(), (byte*)pBuffer);
                StretchDIBits(hdc, 0, 0, Width, Height, 0, 0, Width, Height, pBuffer, pBmi, DIB_RGB_COLORS, SRCCOPY);
            }
            //Sleep(1);    // give some time to other processes
        }

        //ReleaseDC(hwnd, hdc);
        //DestroyWindow(hwnd);

        //ExitProcess(0);
    }

    struct ScreenBuffer
    {
        fixed byte _pixel[Width * Height * 4];
    }
}