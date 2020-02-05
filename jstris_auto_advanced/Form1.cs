using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace jstris_auto_advenced
{
    public partial class Form1 : Form
    {
        #region DllImport
        [DllImport("user32.dll")]
        private static extern int RegisterHotKey(IntPtr hwnd, int id, int fsModifiers, Keys vk);

        [DllImport("kernel32.dll")]
        public static extern bool Beep(int n, int m);

        [DllImport("user32.dll")]
        public static extern int FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("user32.dll")]
        public static extern void keybd_event(uint vk, uint scan, uint flags, uint extraInfo);
        #endregion

        int mode = 1;
        int r = 255, g = 0, b = 0;

        /*
		private void ColorChange()
		{
			switch (mode)
			{
				case 1:
					if (g < 255)
					{
						g += 15;
					}
					else
					{
						r -= 15;
						if (r <= 0)
						{
							mode++;
						}
					}
					break;
				case 2:
					if (b < 255)
					{
						b += 15;
					}
					else
					{
						g -= 15;
						if (g <= 0)
						{
							mode++;
						}
					}
					break;
				case 3:
					if (r < 255)
					{
						r += 15;
					}
					else
					{
						b -= 15;
						if (b <= 0)
						{
							mode -= 2;
						}
					}
					break;
				default:
					break;
			}
			label1.ForeColor = Color.FromArgb(r, g, b);
			label2.ForeColor = Color.FromArgb(r, g, b);
		}
        */

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RegisterHotKey(Handle, 0, 0, Keys.F1);
        }

        private Bitmap CaptureForm()
        {
            #region Capture
            Bitmap bitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(PointToScreen(new Point(pictureBox1.Location.X, pictureBox1.Location.Y)), new Point(0, 0), pictureBox1.Size);
            return bitmap;
            #endregion
        }

        private int GetBlockShape(int color)
        {
            switch (color)
            {
                case 41:
                    return 1;
                case 198:
                    return 2;
                case 228:
                    return 3;
                case 23:
                    return 4;
                case 316:
                    return 5;
                case 90:
                    return 6;
                case 348:
                    return 7;
                default:
                    return 0;
            }
        }

        private double GetHue(int r, int g, int b)
        {
            double R = (double)r / 255;
            double G = (double)g / 255;
            double B = (double)b / 255;
            if (R >= G && R >= B)
            {
                return (G - B) / (R - (G < B ? G : B)) * 60 < 0 ? (G - B) / (R - (G < B ? G : B)) * 60 + 360 : (G - B) / (R - (G < B ? G : B)) * 60;
            }
            else if (G >= R && G >= B)
            {
                return (2.0 + (B - R) / (G - (R < B ? R : B))) * 60 < 0 ? (2.0 + (B - R) / (G - (R < B ? R : B))) * 60 + 360 : (2.0 + (B - R) / (G - (R < B ? R : B))) * 60;
            }
            else
            {
                return (4.0 + (R - G) / (B - (R < G ? R : G))) * 60 < 0 ? (4.0 + (R - G) / (B - (R < G ? R : G))) * 60 + 360 : (4.0 + (R - G) / (B - (R < G ? R : G))) * 60;
            }
        }

        private double GetBrightness(int r, int g, int b)
        {
            double R = (double)r / 255;
            double G = (double)g / 255;
            double B = (double)b / 255;
            if (R >= G && R >= B)
            {
                return (R + (G < B ? G : B)) / 2;
            }
            else if (G >= R && G >= B)
            {
                return (G + (R < B ? R : B)) / 2;
            }
            else
            {
                return (B + (R < G ? R : G)) / 2;
            }
        }

        private unsafe Tuple<int, int, int[,]> DetectMap(Bitmap bitmap)
        {
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            byte* scan0 = (byte*)bitmapData.Scan0.ToPointer();
            int stride = bitmapData.Stride;
            int[,] map = new int[20, 10];
            int falling = 0, save = 0;
            save = GetBlockShape((int)GetHue((scan0 + (60 * stride))[38], (scan0 + (60 * stride))[37], (scan0 + (60 * stride))[36]));
            save = (save == 0 ? 2 : save);
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    int r = (scan0 + ((13 + 24 * i) * stride))[(95 + 24 * j) * 3 + 2];
                    int g = (scan0 + ((13 + 24 * i) * stride))[(95 + 24 * j) * 3 + 1];
                    int b = (scan0 + ((13 + 24 * i) * stride))[(95 + 24 * j) * 3];
                    if (GetBrightness(r, g, b) * 100 > 29)
                    {
                        map[i, j] = 1;
                    }
                    else if (GetBrightness(r, g, b) * 100 > 5 && falling == 0)
                    {
                        falling = GetBlockShape((int)GetHue(r, g, b));
                    }
                }
            }
            bitmap.UnlockBits(bitmapData);
            return new Tuple<int, int, int[,]>(save, falling, map);
        }

        /*
        private Tuple<int, int, int[,]> DetectMap(Bitmap bitmap)
        {
            Color color;
            int[,] map = new int[20, 10];
            int falling = 0, save = 0;
            save = GetBlockShape((int)bitmap.GetPixel(12, 60).GetHue());
            save = (save == 0 ? 2 : save);
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    color = bitmap.GetPixel(95 + 24 * j, 13 + 24 * i);
                    if (color.GetBrightness() * 100 > 29)
                    {
                        map[i, j] = 1;
                    }
                    else if (color.GetBrightness() * 100 > 5 && falling == 0)
                    {
                        falling = GetBlockShape((int)color.GetHue());
                    }
                }
            }
            return new Tuple<int, int, int[,]>(save, falling, map);
        }
        */

        private bool Down(int[,] map_)
        {
            int cnt = 0;
            int[,] map = new int[20, 10];
            Copy(map, map_);
            for (int i = 0; i < 10; i++)
            {
                if (map[19, i] == 2)
                {
                    return false;
                }
            }
            for (int i = 19; i >= 0; i--)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (map[i, j] == 2)
                    {
                        if (map[i + 1, j] != 0)
                        {
                            return false;
                        }
                        cnt++;
                        map[i + 1, j] = 2;
                        map[i, j] = 0;
                    }
                }
            }
            Copy(map_, map);
            if (cnt == 0)
            {
                return false;
            }
            return true;
        }

        private bool Right(int[,] map)
        {
            int cnt = 0;
            for (int i = 0; i < 20; i++)
            {
                if (map[i, 9] == 2)
                {
                    return false;
                }
            }
            for (int i = 0; i < 20; i++)
            {
                for (int j = 9; j >= 0; j--)
                {
                    if (map[i, j] == 2)
                    {
                        cnt++;
                        map[i, j + 1] = 2;
                        map[i, j] = 0;
                    }
                }
            }
            if (cnt == 0)
            {
                return false;
            }
            return true;
        }

        private bool Left(int[,] map)
        {
            int cnt = 0;
            for (int i = 0; i < 20; i++)
            {
                if (map[i, 0] == 2)
                {
                    return false;
                }
            }
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (map[i, j] == 2)
                    {
                        cnt++;
                        map[i, j - 1] = 2;
                        map[i, j] = 0;
                    }
                }
            }
            if (cnt == 0)
            {
                return false;
            }
            return true;
        }

        private void DoBreak(int[,] map)
        {
            for (int i = 0; i < 20; i++)
            {
                int cnt = 0;
                for (int j = 0; j < 10; j++)
                {
                    if (map[i, j] != 0)
                    {
                        cnt++;
                    }
                }
                if (cnt == 10)
                {
                    for (int j = i; j > 0; j--)
                    {
                        for (int k = 0; k < 10; k++)
                        {
                            map[j, k] = map[j - 1, k];
                        }
                    }
                    for (int j = 0; j < 10; j++)
                    {
                        map[0, j] = 0;
                    }
                    i--;
                }
            }

        }

        private Tuple<int, int, int> Score(int[,] map_) // score, move, space_count
        {
            int min = int.MaxValue, move = 0, cnt = 0, min_space = 0;
            int[,] map = new int[20, 10];
            while (Left(map_)) ;
            while (true)
            {
                Copy(map, map_);
                while (Down(map)) ;
                int calc = 0;
                DoBreak(map);
                #region GetHight
                int hight_count = 0;
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 20; j++)
                    {
                        if (map[j, i] != 0)
                        {
                            hight_count += (20 - j) * (20 - j);
                            break;
                        }
                    }
                }
                #endregion
                #region Space
                int space_count = 0;
                for (int i = 0; i < 10; i++)
                {
                    bool space = false;
                    for (int j = 0; j < 20; j++)
                    {
                        if (map[j, i] != 0)
                        {
                            space = true;
                        }
                        if (map[j, i] == 0 && space)
                        {
                            space_count++;
                        }
                    }
                }
                calc += hight_count;
                calc += space_count / (hight_count == 0 ? 1 : hight_count) * 4000;
                #endregion
                if (calc < min)
                {
                    min = calc;
                    move = cnt;
                    min_space = space_count;
                }
                if (!Right(map_))
                {
                    return new Tuple<int, int, int>(min, move, min_space);
                }
                cnt++;
            }
        }

        private void Copy(int[,] dest, int[,] from)
        {
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    dest[i, j] = from[i, j];
                }
            }
        }

        private bool isContinuable(int[,] a)
        {
            #region GetMap
            Bitmap bitmap = CaptureForm();
            if (bitmap == null)
            {
                return true;
            }
            int[,] b = DetectMap(bitmap).Item3;
            #endregion
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (a[i, j] != b[i, j])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private void Bruteforce(int save, int falling, int[,] map_)
        {
            bool isSave = false;
            int min = int.MaxValue, rotate = 0, move = 0, space_cnt = 0;
            int[,] map = new int[20, 10];
            int[,,] array1 = new int[4, 4, 4];
            int[,,] array2 = new int[4, 4, 4];
            #region falling
            switch (falling)
            {
                case 1:
                    array1 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 1, 0 }
                        }
                    };
                    break;
                case 2:
                    array1 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {1, 1, 1, 1 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 0, 1, 0 },
                            {0, 0, 1, 0 },
                            {0, 0, 1, 0 },
                            {0, 0, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {1, 1, 1, 1 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 }
                        }
                    };
                    break;
                case 3:
                    array1 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {1, 0, 0, 0 },
                            {1, 1, 1, 0 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {1, 1, 1, 0 },
                            {0, 0, 1, 0 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 },
                            {1, 1, 0, 0 }
                        }
                    };
                    break;
                case 4:
                    array1 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 1, 0 },
                            {1, 1, 1, 0 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {1, 1, 1, 0 },
                            {1, 0, 0, 0 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {1, 1, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 }
                        }
                    };
                    break;
                case 5:
                    array1 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {1, 1, 1, 0 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {1, 1, 1, 0 },
                            {0, 1, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {1, 1, 0, 0 },
                            {0, 1, 0, 0 }
                        }
                    };
                    break;
                case 6:
                    array1 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {1, 1, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 0, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {1, 1, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {1, 0, 0, 0 },
                            {1, 1, 0, 0 },
                            {0, 1, 0, 0 }
                        }
                    };
                    break;
                case 7:
                    array1 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {1, 1, 0, 0 },
                            {0, 1, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 1, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {1, 1, 0, 0 },
                            {0, 1, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {1, 1, 0, 0 },
                            {1, 0, 0, 0 }
                        }
                    };
                    break;
            }
            for (int i = 0; i < 4; i++)
            {
                Copy(map, map_);
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        map[j, k + 3] = (array1[i, j, k] == 1 ? 2 : 0);
                    }
                }
                var ret = Score(map);
                if (ret.Item1 < min)
                {
                    min = ret.Item1;
                    rotate = i;
                    move = ret.Item2;
                    space_cnt = ret.Item3;
                }
            }
            #endregion
            #region save
            switch (save)
            {
                case 1:
                    array2 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 1, 0 }
                        }
                    };
                    break;
                case 2:
                    array2 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {1, 1, 1, 1 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 0, 1, 0 },
                            {0, 0, 1, 0 },
                            {0, 0, 1, 0 },
                            {0, 0, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {1, 1, 1, 1 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 }
                        }
                    };
                    break;
                case 3:
                    array2 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {1, 0, 0, 0 },
                            {1, 1, 1, 0 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {1, 1, 1, 0 },
                            {0, 0, 1, 0 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 },
                            {1, 1, 0, 0 }
                        }
                    };
                    break;
                case 4:
                    array2 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 1, 0 },
                            {1, 1, 1, 0 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {1, 1, 1, 0 },
                            {1, 0, 0, 0 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {1, 1, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 0, 0 }
                        }
                    };
                    break;
                case 5:
                    array2 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {1, 1, 1, 0 },
                            {0, 0, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {1, 1, 1, 0 },
                            {0, 1, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {1, 1, 0, 0 },
                            {0, 1, 0, 0 }
                        }
                    };
                    break;
                case 6:
                    array2 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {1, 1, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {0, 1, 1, 0 },
                            {0, 0, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {0, 1, 1, 0 },
                            {1, 1, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {1, 0, 0, 0 },
                            {1, 1, 0, 0 },
                            {0, 1, 0, 0 }
                        }
                    };
                    break;
                case 7:
                    array2 = new int[4, 4, 4]
                    {
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {1, 1, 0, 0 },
                            {0, 1, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 1, 0 },
                            {0, 1, 1, 0 },
                            {0, 1, 0, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 0, 0, 0 },
                            {1, 1, 0, 0 },
                            {0, 1, 1, 0 }
                        },
                        {
                            {0, 0, 0, 0 },
                            {0, 1, 0, 0 },
                            {1, 1, 0, 0 },
                            {1, 0, 0, 0 }
                        }
                    };
                    break;
            }
            for (int i = 0; i < 4; i++)
            {
                Copy(map, map_);
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        map[j, k + 3] = (array2[i, j, k] == 1 ? 2 : 0);
                    }
                }
                var ret = Score(map);
                if (ret.Item1 < min)
                {
                    isSave = true;
                    min = ret.Item1;
                    rotate = i;
                    move = ret.Item2;
                    space_cnt = ret.Item3;
                }
            }
            #endregion
            #region CalculateMove
            bool calc_continue = true;
            move -= 3;
            for (int i = 0; i < 4 && calc_continue; i++)
            {
                int j;
                for (j = 0; j < 4; j++)
                {
                    if ((isSave ? array2 : array1)[rotate, j, i] != 0)
                    {
                        calc_continue = false;
                        break;
                    }
                }
                if (j == 4)
                {
                    move--;
                }
            }
            #endregion
            /*
			//MessageBox.Show("isSave = " + (isSave ? "True" : "False") + "\nrotate = " + rotate.ToString() + "\nmove = " + move.ToString());
			string text1 = "BEST VALUE=" + min.ToString() + "\nisSave=" + (isSave ? "True" : "False") + "\nrotate=" + rotate.ToString() + "\nmove=" + move.ToString() + "\nspace_cnt=" + space_cnt.ToString() + "\n\n";
			for (int i = 0; i < 4; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					text1 += (isSave ? (array2[0, i, j] == 0 ? "□" : "■") : (array1[0, i, j] == 0 ? "□" : "■"));
				}
				text1 += "\n";
			}
			label1.Text = text1;
			string text2 = "";
			for (int i = 0; i < 20; i++)
			{
				for (int j = 0; j < 10; j++)
				{
					text2 += (map_[i, j] == 0 ? "□" : "■");
				}
				text2 += "\n";
			}
			label2.Text = text2;
            */
            if (isSave)
            {
                keybd_event((byte)Keys.C, 0, 0x00, 0);
                Thread.Sleep(1);
                keybd_event((byte)Keys.C, 0, 0x02, 0);
                Thread.Sleep(1);
            }
            if (rotate > 2)
            {
                for (int i = 0; i < rotate - 2; i++)
                {
                    keybd_event((byte)Keys.Z, 0, 0x00, 0);
                    Thread.Sleep(1);
                    keybd_event((byte)Keys.Z, 0, 0x02, 0);
                    Thread.Sleep(1);
                }
            }
            else
            {
                for (int i = 0; i < rotate; i++)
                {
                    keybd_event((byte)Keys.Up, 0, 0x00, 0);
                    Thread.Sleep(1);
                    keybd_event((byte)Keys.Up, 0, 0x02, 0);
                    Thread.Sleep(1);
                }
            }
            for (int i = move; i < 0; i++)
            {
                keybd_event((byte)Keys.Left, 0, 0x00, 0);
                Thread.Sleep(1);
                keybd_event((byte)Keys.Left, 0, 0x02, 0);
                Thread.Sleep(1);
            }
            for (int i = 0; i < move; i++)
            {
                keybd_event((byte)Keys.Right, 0, 0x00, 0);
                Thread.Sleep(1);
                keybd_event((byte)Keys.Right, 0, 0x02, 0);
                Thread.Sleep(1);
            }
            keybd_event((byte)Keys.Space, 0, 0x00, 0);
            Thread.Sleep(1);
            keybd_event((byte)Keys.Space, 0, 0x02, 0);
            Thread.Sleep(1);
            while (isContinuable(map_)) ;
            Application.DoEvents();
            //Thread.Sleep(1000);
            //ColorChange();
        }

        private void run()
        {
            while (running)
            {
                #region Capture
                Bitmap bitmap = CaptureForm();
                if (bitmap == null)
                {
                    continue;
                }
                #endregion
                #region DetectMap
                var ret = DetectMap(bitmap);
                int save = ret.Item1;
                int falling = ret.Item2;
                int[,] map = ret.Item3;
                #endregion
                Bruteforce(save, falling, map);
                Application.DoEvents();
            }
        }

        bool running = false;

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x312:
                    var key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                    if (key == Keys.F1)
                    {
                        running = !running;
                        if (running)
                        {
                            new Thread(Delegate => Beep(2000, 200)).Start();
                            run();
                        }
                        else
                        {
                            new Thread(Delegate => Beep(1000, 200)).Start();
                        }
                    }
                    break;
                default:
                    break;
            }
            base.WndProc(ref m);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}

/*
HUE
■■	41.86667
■■

■■■■	198

■	228.3636
■■■

    ■	23.73333
■■■

  ■	316.5672
■■■

  ■■	90
■■

■■	348
  ■■
*/
