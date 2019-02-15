using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DxLibDLL;


namespace ShowMV1Shape
{
    public partial class Form1 : Form
    {
        string APP_TITLE = "ShowMV1Shape ver. 0.2019.02.15.04";
        string sMV1File = "";
        int nID = -1;
        int SIZE_WIDTH = 1280, SIZE_HEIGHT = 720;
        bool bOK = false;
        float fZoom = 1.0F, fRotXY = 0.0F, fRotYZ = 0.0F, fPosX = 0.0F, fPosY = 0.0F;
        int nPosX = 0, nPosY = 0;
        bool bMouseDnD = false;
        bool bUseDX9 = true;

        public Form1()
        {
            InitializeComponent();

            // トレーダー分岐点
            if (MessageBox.Show("DirectX 11が使用できる場合は使用しますか？" + Environment.NewLine + Environment.NewLine +
                "アンチエイリアシングは効きますが、ウィンドウサイズ変更はできません。DirecｔX 9はその逆です。" + ")",
                APP_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                bUseDX9 = false;
            }

            // ホイール処理は手動でハンドラ追加する必要あり
            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(Form1_MouseWheel);

            this.Text = APP_TITLE + " ";
            this.ClientSize = new Size(SIZE_WIDTH, SIZE_HEIGHT);
            DX.SetUserWindow(this.Handle);
            DX.ChangeWindowMode(DX.TRUE);
            DX.SetWindowSizeChangeEnableFlag(DX.TRUE, DX.FALSE);
            DX.SetWindowSize(SIZE_WIDTH, SIZE_HEIGHT);

            DX.SetAlwaysRunFlag(DX.TRUE);
            DX.SetMultiThreadFlag(DX.TRUE);

            DX.SetZBufferBitDepth(24); // デフォルト(16)では最近は厳しいので
            DX.SetCreateDrawValidGraphZBufferBitDepth(24); // こっちは不要だけどまぁ習慣づけ
            Point pt = Cursor.Position;
            DX.SetGraphMode(Screen.GetBounds(pt).Width, Screen.GetBounds(pt).Height, 32);

            // C#(Form使用) + DX9では効かないっぽい
            DX.SetFullSceneAntiAliasingMode(4, 2);
            DX.SetDrawValidMultiSample(4, 2);

            // C#(Form)+DX11ではウィンドウサイズ変更に追従できないのでDX9を使用
            if (bUseDX9 == true)
            {
                DX.SetUseDirect3D9Ex(DX.TRUE);
            }
            else
            {
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
            }
            DX.SetDrawMode(DX.DX_DRAWMODE_BILINEAR);

            bOK = (DX.DxLib_Init() == 0); // エラーが出てもここではreturnしない
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (bOK == false)
            {
                MessageBox.Show("エラーが発生しました。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close(); // これで終了
            }
            else
            {
                DX.SetDrawScreen(DX.DX_SCREEN_BACK); // これ、つい忘れる。なお、DxLib_Initの前だと無意味なので注意
                bOK = true;
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            ResetValues();
            UpdateFrame();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            DX.SetWindowSize(ClientSize.Width, ClientSize.Height);
            UpdateFrame();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DX.MV1InitModel();
            DX.DxLib_End();
        }

        void UpdateFrame()
        {
            if (bOK == false) return;

            int X1 = 82;
            if (nID < 0)
            {
                string s = "MV1ファイルをドラッグ＆ドロップしてください。";
                int x1 = DX.GetDrawStringWidth(s, s.Length);
                DX.ClearDrawScreen();
                DX.DrawString((ClientSize.Width - X1 - x1) / 2 + X1, 240, s, DX.GetColor(255, 255, 0));
                DX.ScreenFlip();
                return;
            }

            DX.MV1DetachAnim(nID, 0); // 状況はいろいろあるのでとりまあろうがなかろうがDetach
            DX.MV1AttachAnim(nID, listBox1.SelectedIndex);
            DX.MV1SetAttachAnimTime(nID, 0, (float)hScrollBar1.Value);

            DX.ClearDrawScreen();

            DX.SetCameraNearFar(0.1f, 1000.0f);
            DX.SetCameraPositionAndTarget_UpVecY(DX.VGet(0.0f, 19.0f, -22.5f), DX.VGet(0.0f, 10.0f, 0.0f));

            float DX_PI_F = 3.1415926535F; // C#版では定義されていない模様 (DX.DX_PI_F もない)
            DX.VECTOR vct = new DX.VECTOR();
            vct.x = fRotYZ * DX_PI_F / 180.0f;
            vct.y = fRotXY * DX_PI_F / 180.0f;
            vct.z = 0.0F;
            DX.MV1SetRotationXYZ(nID, vct); // 回転
            vct.x = fZoom;
            vct.y = fZoom;
            vct.z = fZoom;
            DX.MV1SetScale(nID, vct); // 拡大縮小
            vct.x = fPosX;
            vct.y = fPosY;
            vct.z = 0.0F;
            DX.MV1SetPosition(nID, vct); // 移動

            DX.MV1DrawModel(nID);

            DX.DrawString(24, 8, hScrollBar1.Value.ToString(), DX.GetColor(255, 255, 255));

            int n = DX.MV1GetShapeNum(nID);
            int x = X1, y = 30;
            for(int i=0; i<n; i++)
            {
                string s = i.ToString("000:");
                DX.DrawString(x, y, s, DX.GetColor(0, 255, 255));
                DX.DrawString(x + DX.GetDrawStringWidth(s, s.Length), y, DX.MV1GetShapeName(nID, i), DX.GetColor(255, 255, 255));

                float f = DX.MV1GetShapeApplyRate(nID, i); // この関数が追加(2019/02/14)されたからこのアプリがある
                s = f.ToString("0.000");
                DX.DrawString(x + 160, y, s, DX.GetColor(255, 255, (f == 0.0F) ?  255 : 0));

                y += 20;
                if (y > ClientSize.Height - 16)
                {
                    y = 30; x += 240;
                }
            }
            DX.DrawString(920, 8, "R横:" + fRotXY.ToString("0.00") + ", R縦:" + fRotYZ.ToString("0.00") + ", Zm:" + fZoom.ToString("0.00") + ", Y:" + fPosY.ToString("0.00"), DX.GetColor(255, 255, 255));

            DX.ScreenFlip();
        }

        void ResetValues()
        {
            fZoom = 1.0F;
            bMouseDnD = false;
            fRotXY = fRotYZ = fPosX = fPosY = 0.0F;
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (nID < 0) return;

            int n = listBox1.SelectedIndex;
            float f = DX.MV1GetAnimTotalTime(nID, n);
            hScrollBar1.Minimum = 0;
            hScrollBar1.Maximum = (int)f + (hScrollBar1.LargeChange - 1); // ミスりやすい点だ
            hScrollBar1.Value = 0;
            ResetValues();
            UpdateFrame();
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            if (nID < 0) return;
            UpdateFrame();
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (nID > 0)
            {
                listBox1.Items.Clear();
                DX.MV1InitModel();
                nID = -1;
                this.Text = APP_TITLE + " ";
            }
            string[] fileName = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            sMV1File = fileName[0]; // 先頭のファイルしか興味なし
            nID = DX.MV1LoadModel(sMV1File);
            if (nID < 0)
            {
                MessageBox.Show("このファイルは読み込めません。", APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            this.Text = APP_TITLE + " - " + sMV1File;

            for (int i = 0; i < DX.MV1GetAnimNum(nID); i++)
            {
                String s = DX.MV1GetAnimName(nID, i);
                listBox1.Items.Add(i + ":" + s);
                listBox1.SelectedIndex = 0;
            }
            UpdateFrame();
        }

        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            int d = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
            if (d > 0)
            {
                fZoom += (float)d * 0.05F; // ホイールの回転方向はお好みで
            }
            else
            {
                fZoom -= -(float)d * 0.05F;
            }
            if (fZoom < 0.05F) fZoom = 0.05F;
            UpdateFrame();
        }

        private void Form1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (nID < 0) return;
            ResetValues();
            UpdateFrame();
        }

        private void listBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (nID < 0) return;

            Keys k = e.KeyCode & Keys.KeyCode;
            e.Handled = true; // これによりリストボックスのキー入力は無かったものになる

            int n = hScrollBar1.Value;
            if (k == Keys.PageUp) n += 50;
            if (k == Keys.PageDown) n -= 50;
            if (k == Keys.Right) n++;
            if (k == Keys.Left) n--;
            if (k == Keys.Home) n = 0;
            if (k == Keys.End) n = (int)DX.MV1GetAnimTotalTime(nID, listBox1.SelectedIndex);
            if (n < 0) n = 0;
            if (n > (int)DX.MV1GetAnimTotalTime(nID, listBox1.SelectedIndex)) n = (int)DX.MV1GetAnimTotalTime(nID, listBox1.SelectedIndex);

            hScrollBar1.Value = n;
            UpdateFrame();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (nID < 0) return;
            if (bMouseDnD == false) bMouseDnD = true;
            nPosX = e.X;
            nPosY = e.Y;
            UpdateFrame();
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (nID < 0) return;
            if (bMouseDnD == true) bMouseDnD = false;
            UpdateFrame();
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (nID < 0) return;
            if (bMouseDnD == false) return;

            int nX = 0, nY = 0;
            float fX = 0.0F, fY = 0.0F;

            // マウスの動きと動く向きの調整はこのあたりで
            if (e.Button == MouseButtons.Left)
            {
                nX = nPosX - e.X;
            }
            if (e.Button == MouseButtons.Right)
            {
                nY = nPosY - e.Y;
            }
            if (e.Button == MouseButtons.Middle)
            {
                fX = nPosX - e.X;
                fY = nPosY - e.Y;
            }

            fRotXY += (float)nX / 2.0F;
            fRotYZ += (float)nY / 2.0F;
            fPosX -= fX / 8.0F;
            fPosY += fY / 4.0F;
            UpdateFrame();

            nPosX = e.X;
            nPosY = e.Y;
        }
    }
}
