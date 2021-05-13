using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SnippingTool169
{
    public partial class form : Form
    {
        Bitmap ScreenImage;
        Rectangle ImageInPicture;
        Rectangle SnipSize = new Rectangle(0, 0, 640, 360);
        Rectangle oldSnipSize;
        Point Aspect = new Point(16,9);
        double Zoom;

        public form()
        {
            InitializeComponent();

            //全スクリーンサイズの確定
            var rect = new Rectangle(0, 0, 0, 0);

            foreach (var scr in Screen.AllScreens)
            {
                rect = MargeRect(rect, scr.Bounds);
            }

            ScreenImage = new Bitmap(rect.Width, rect.Height);

            //プリントスクリーン
            using (var g = Graphics.FromImage(ScreenImage))
            {
                g.CopyFromScreen(new Point(rect.Left, rect.Top), new Point(0, 0), ScreenImage.Size);
            }


            oldSnipSize = SnipSize;

            //位置変更
#if !DEBUG
            this.Location = new Point(rect.Left, rect.Top);
            this.Size = new Size(rect.Width, rect.Height);
#else

            resize();
#endif


        }

        private Rectangle MargeRect(Rectangle rect1, Rectangle rect2)
        {
            int left = Math.Min(rect1.Left, rect2.Left);
            int top = Math.Min(rect1.Top, rect2.Top);
            int right = Math.Max(rect1.Right, rect2.Right);
            int bottom = Math.Max(rect1.Bottom, rect2.Bottom);

            return new Rectangle(left, top, right - left, bottom - top);
        }
        private Rectangle ExpandRect(Rectangle rect, int ex)
        {
            return new Rectangle(
                rect.Left - ex, rect.Top - ex,
                rect.Width + ex * 2, rect.Height + ex * 2);
        }


        private void form_Resize(object sender, EventArgs e)
        {
            resize();
        }

        private void resize()
        {
            //アスペクト比の計算
            var bmp_ratio = (double)(ScreenImage.Size.Width) / (double)(ScreenImage.Size.Height);
            var box_ratio = (double)(pictureBox.Size.Width) / (double)(pictureBox.Size.Height);

            //比率に応じて新しい寸法を計算
            if (bmp_ratio < box_ratio)
            {//SSの方がウインドウよりタテ長
                int width = (int)(pictureBox.Size.Height * bmp_ratio);
                ImageInPicture = new Rectangle(
                    (pictureBox.Size.Width - width) / 2, 0,
                    width, pictureBox.Size.Height);
                Zoom = (double)(pictureBox.Size.Height) / (double)(ScreenImage.Size.Height);
            }
            else
            {//SSの方がウインドウよりヨコ長
                int height = (int)(pictureBox.Size.Width / bmp_ratio);
                ImageInPicture = new Rectangle(
                    0, (pictureBox.Size.Height - height) / 2,
                    pictureBox.Size.Width, height);
                Zoom = (double)(pictureBox.Size.Width) / (double)(ScreenImage.Size.Width);
            }

            //ビットマップ作成
            var img = new Bitmap(pictureBox.Size.Width, pictureBox.Size.Height);
            using (var g = Graphics.FromImage(img))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                //色調補正
                float scale = 0.8f;
                float append = 0.5f * (1f - scale) + 0.10f;
                var color = new ColorMatrix(
                        new float[][] {
                        new float[] {scale, 0, 0, 0, 0},
                        new float[] {0, scale, 0, 0, 0},
                        new float[] {0, 0, scale, 0, 0},
                        new float[] {0, 0, 0, 1, 0},
                        new float[] {append, append, append, 0, 1}
                });

                var attr = new ImageAttributes();
                attr.SetColorMatrix(color);

                g.DrawImage(ScreenImage,
                    ImageInPicture,
                    0, 0, ScreenImage.Size.Width, ScreenImage.Size.Height,
                    GraphicsUnit.Pixel, attr);
            }

            pictureBox.Image = img;
        }

        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            //切り取り領域を描画
            using (var pen = new Pen(Color.Red, 2))
            {
                var pos = new Point(
                    (int)(ImageInPicture.Left + SnipSize.Left * Zoom),
                    (int)(ImageInPicture.Top + SnipSize.Top * Zoom));
                var size = new Size(
                    (int)((SnipSize.Right - SnipSize.Left) * Zoom),
                    (int)((SnipSize.Bottom - SnipSize.Top) * Zoom));

                var rect = new Rectangle(pos, size);
                e.Graphics.DrawRectangle(pen, rect);
            }

        }
        private void MyReflesh()
        {
            var rect = MargeRect(oldSnipSize, SnipSize);

            rect = new Rectangle(
                rect.X * ImageInPicture.Width / ScreenImage.Size.Width + ImageInPicture.X,
            rect.Y * ImageInPicture.Height / ScreenImage.Size.Height + ImageInPicture.Y,
            rect.Width * ImageInPicture.Width / ScreenImage.Size.Width,
            rect.Height * ImageInPicture.Height / ScreenImage.Size.Height);

            rect = ExpandRect(rect, 10);


            pictureBox.Invalidate(rect);
            oldSnipSize = SnipSize;
            pictureBox.Update();
        }

        private void form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
        private void form_MouseWheel(object sender, MouseEventArgs e)
        {
            var delta = e.Delta * -0.1;
            ResizeSnipSize(delta);
        }
        private void ResizeSnipSize(double delta)
        {
            var width = SnipSize.Size.Width + delta;
            SnipSize.Size = new Size(
                (int)(width),
                (int)(width * Aspect.Y / Aspect.X));

            MyReflesh();
        }


        private void form_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    form_LeftMouseDown();
                    break;
                case MouseButtons.Right:
                    form_RightMouseDown();
                    break;
            }

        }

        private void form_LeftMouseDown()
        {
            //ビットマップ作成
            var img = new Bitmap(SnipSize.Width, SnipSize.Height);
            using (var g = Graphics.FromImage(img))
            {
                g.DrawImage(ScreenImage,
                    new Rectangle(0, 0, SnipSize.Width, SnipSize.Height), SnipSize, GraphicsUnit.Pixel);
            }

            //ファイルパス作成
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            for (int i = 0; true; i++)
            {
                var file = $"{desktop}{Path.DirectorySeparatorChar}snip_{i}.png";
                if (!File.Exists(file))
                {
                    img.Save(file, ImageFormat.Png);
                    break;

                }
            }
            //終了。
            this.Close();
        }
        private void form_RightMouseDown()
        {
            var tmp = Aspect.Y;
            Aspect.Y = Aspect.X;
            Aspect.X = tmp;

            SnipSize.Size = new Size(
                SnipSize.Size.Height,
                SnipSize.Size.Width);

            ResizeSnipSize(0);
        }

        private void form_MouseMove(object sender, MouseEventArgs e)
        {
            int posx = e.Location.X - ImageInPicture.Left;
            int posy = e.Location.Y - ImageInPicture.Top;

            posx = posx * ScreenImage.Size.Width / ImageInPicture.Width;
            posy = posy * ScreenImage.Size.Height / ImageInPicture.Height;

            SnipSize.Location = new Point(
                posx - SnipSize.Size.Width / 2,
                posy - SnipSize.Size.Height / 2
                );

            MyReflesh();
        }
    }
}
