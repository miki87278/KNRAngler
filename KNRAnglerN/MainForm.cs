﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KNRAnglerN
{
    public partial class MainForm : Form
    {
        public const string ver = "7.1"; 
        public readonly ConsoleForm consoleForm;
        public readonly SettingsForm settingsForm;
        public OkonClient okonClient;
        public int requestedVideoFeedFrames = 0;
        public int requestedDepthMapFrames = 0;
        public int framesNum = 0;
        public int ping = 0;
        DateTime framesLastCheck = DateTime.Now;

        public enum Packet : byte
        {
            SET_MTR = 0xA0,
            GET_SENS = 0xB0,
            GET_DEPTH = 0xB1,
            GET_DEPTH_BYTES = 0xB2,
            GET_VIDEO_BYTES = 0xB3,
            SET_SIM = 0xC0,
            ACK = 0xC1,
            GET_ORIEN = 0xC2,
            SET_ORIEN = 0xC3,
            REC_STRT = 0xD0,
            REC_ST = 0xD1,
            REC_RST = 0xD2,
            GET_REC = 0xD3,
            PING = 0xC5,
            GET_DETE = 0xDE
        }

        [Flags]
        private enum Flag
        {
            None = 0,
            SERVER_ECHO = 1,
            DO_NOT_LOG_PACKET = 2,
            TEST = 128
        }
        public MainForm()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            consoleForm = new ConsoleForm(this);
            settingsForm = new SettingsForm(this);
            Text = "KNR Wędkarz - Okoń Sim control v" + ver + " by Vectro 2020";
        }

        public class Info : OkonClient.IInfo
        {
            public MainForm mainFormInstance;
            public Info(MainForm instance) => this.mainFormInstance = instance;
            public void YeetLog(string info)
            {
                if (mainFormInstance.settingsForm.chkYeetLog.Checked)
                    mainFormInstance.consoleForm.txtConsole.AppendText("[LOG]: " + info + Environment.NewLine);
            }
            public void YeetException(Exception exp)
            {
                if (exp.Message.ToLower().Contains("aborted")) return;
                mainFormInstance.consoleForm.txtConsole.AppendText("[ERR]: " + exp.Message + Environment.NewLine);
                mainFormInstance.consoleForm.txtConsole.AppendText("[ERR]: " + exp.StackTrace + Environment.NewLine);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //  this.BeginInvoke((Action)(() => MessageBox.Show("Warning!\nDevelopment build, some components may not work")));  

           


        }

        public void HandleReceivedPacket(object o, OkonClient.PacketEventArgs e)
        {
            int maxLength = 0;
            foreach (var s in Enum.GetNames(typeof(Packet))) maxLength = Math.Max(maxLength, s.Length);
            switch ((Packet)e.packetType)
            {
                case Packet.GET_DEPTH:
                    consoleForm.Log = " RECV[" + Enum.GetName(typeof(Packet), e.packetType).PadRight(maxLength) + "] " + Encoding.ASCII.GetString(e.packetData, 0, e.packetData.Length);
                    var json = Utf8Json.JsonSerializer.Deserialize<dynamic>(Encoding.ASCII.GetString(e.packetData));
                    MemoryStream ms = new MemoryStream(Convert.FromBase64String(json["depth"]));
                    Image img = picDepthMap.Image;
                    picDepthMap.Image = Image.FromStream(ms);
                    if (img != null) img.Dispose();
                    break;
                case Packet.GET_DEPTH_BYTES:
                    requestedDepthMapFrames--;
                    //Log = " RECV[" + Enum.GetName(typeof(Packet), e.packetType).PadRight(maxLength) + "] " + "size: " + e.packetData.Length +"B";
                    ms = new MemoryStream(e.packetData);
                    img = picDepthMap.Image;
                    picDepthMap.Image = Image.FromStream(ms);
                    if (img != null) img.Dispose();
                    framesNum++;
                    break;
                case Packet.GET_VIDEO_BYTES:
                    requestedVideoFeedFrames--;
                    //Log = " RECV[" + Enum.GetName(typeof(Packet), e.packetType).PadRight(maxLength) + "] " + "size: " + e.packetData.Length +"B";
                    ms = new MemoryStream(e.packetData);
                    img = pictureBox1.Image;
                    pictureBox1.Image = Image.FromStream(ms);
                    if (img != null) img.Dispose();
                    break;
                case Packet.PING:
                    json = Utf8Json.JsonSerializer.Deserialize<dynamic>(Encoding.ASCII.GetString(e.packetData));
                    ping =(int)json["ping"];
                    break;
                default:
                    consoleForm.Log = " RECV[" + Enum.GetName(typeof(Packet), e.packetType).PadRight(maxLength) + "] " + Encoding.ASCII.GetString(e.packetData, 0, e.packetData.Length);
                    break;
            }
        }

        private void tmrFrameRate_Tick(object sender, EventArgs e)
        {
            if (okonClient != null && okonClient.IsConnected() && settingsForm.chkVideoFeed.Checked) {
                if (requestedVideoFeedFrames < 2)
                    try
                    {
                        okonClient.SendString((byte)Packet.GET_VIDEO_BYTES, (byte)Flag.DO_NOT_LOG_PACKET, "");
                        requestedVideoFeedFrames++;
                    }
                    catch
                    {
                        consoleForm.Log = "Error, packet not sent";
                        settingsForm.chkVideoFeed.Checked = false;
                    }
                if (requestedDepthMapFrames < 2)
                    try
                    {
                        okonClient.SendString((byte)Packet.GET_DEPTH_BYTES, (byte)Flag.DO_NOT_LOG_PACKET, "");
                        requestedDepthMapFrames++;
                    }
                    catch
                    {
                        consoleForm.Log = "Error, packet not sent";
                        settingsForm.chkVideoFeed.Checked = false;
                    }
            } 
        }
        bool[] control = { false, false, false, false, false, false};
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (okonClient == null || !okonClient.IsConnected() || !settingsForm.chkManualControl.Checked) return;

            if (e.KeyCode == Keys.W) control[0] = true;
            if (e.KeyCode == Keys.A) control[1] = true;
            if (e.KeyCode == Keys.S) control[2] = true;
            if (e.KeyCode == Keys.D) control[3] = true;
            if (e.KeyCode == Keys.Q) control[4] = true;
            if (e.KeyCode == Keys.E) control[5] = true;
            UpdateManualControl();
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {   
            if (okonClient == null || !okonClient.IsConnected() || !settingsForm.chkManualControl.Checked) return;
            if (e.KeyCode == Keys.W) control[0] = false;
            if (e.KeyCode == Keys.A) control[1] = false;
            if (e.KeyCode == Keys.S) control[2] = false;
            if (e.KeyCode == Keys.D) control[3] = false;
            if (e.KeyCode == Keys.Q) control[4] = false;
            if (e.KeyCode == Keys.E) control[5] = false;
            UpdateManualControl();
        }

        private void UpdateManualControl()
        {//W A S D  Q E
            float vel = 0, dir = 0, fr = 0, ba = 0;
            if (control[0] && !control[2]) vel = 1f;
            if (!control[0] && control[2]) vel = -1f;
            if (control[1] && !control[3]) dir = -1f;
            if (!control[1] && control[3]) dir = 1f;
            if (control[4] && !control[5]) { fr = 1f; ba = -0.7f; }
            if (!control[4] && control[5]) { fr = -0.7f;  ba = 1f; }
            float l = Math.Max(Math.Min((1 * dir + 1 * vel), 1), -1);
            float r = Math.Max(Math.Min((-1 * dir + 1 * vel), 1), -1);
            try
            {
                okonClient.SendString((byte)Packet.SET_MTR, (byte)Flag.DO_NOT_LOG_PACKET, "{\"FL\":" + fr.ToString().Replace(',','.') + ",\"FR\":" + fr.ToString().Replace(',', '.') + ",\"ML\":" + l.ToString().Replace(',', '.') + ",\"MR\":" + r.ToString().Replace(',', '.') + ",\"B\":" + ba.ToString().Replace(',', '.') + "}");
            }
            catch
            {
                settingsForm.chkManualControl.Checked = false;
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            if (settingsForm.Visible) settingsForm.Hide();
            else settingsForm.Show();
        }

        private void btnShowConsole_Click(object sender, EventArgs e)
        {
            if (consoleForm.Visible) consoleForm.Hide();
            else consoleForm.Show();
        }

        private void tmrFramerate_Tick_1(object sender, EventArgs e)
        {
            lblFrameRate.Text = Math.Round((1000.0 * framesNum / (DateTime.Now.Subtract(framesLastCheck).TotalMilliseconds))).ToString() + "FPS ping " + ping + "ms";

            framesLastCheck = DateTime.Now;
            framesNum = 0;
            if (okonClient != null && okonClient.IsConnected())
            {
                try
                {
                    long time = (System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond);
                    okonClient.SendString((byte)Packet.PING, (byte)Flag.DO_NOT_LOG_PACKET, "{\"timestamp\":" + time + ",\"ping\":0}");
                }
                catch { }
            }

        }
        
        double time = 0;
        private void tmrHUD_Tick(object sender, EventArgs e)
        {
            int start = DateTime.Now.Millisecond;
            int W = pictureBox1.Width;
            int H = pictureBox1.Height;
            Bitmap b = new Bitmap(W, H);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.FillRectangle(Brushes.Black, 0, 0, W, H);
                g.DrawLine(Pens.Green, 0, H / 2, W, H / 2);
                g.DrawLine(Pens.Green, W/2, 0, W/2, H);
                Pen green = new Pen(Brushes.Green, 3);

                {//HEADING
                    float w = 0.5f; 
                    float h = 0.06f;
                    float hfov = 90; //horizontal fov
                    float fontHeight = 20;
                    Font f = new Font("Inconsolata",fontHeight * 0.6f, FontStyle.Bold);
                    float fontWidth = g.MeasureString("000", f).Width / 3;
                    float fontBottom = H * h * 0.3f;
                    float hdg = x;//(float)( ( 30+90*Math.Sin(time*0.1) )%360 );
                    //if (hdg < 0) hdg += 360f;
                    float top = 0.01f;
                    float smallLine = H * h * 0.3f;
                    float bigLine = H * h * 0.5f;
                    //g.DrawRectangle(green, W / 2 - W*w/2, H*top, W*w, H*h);
                    g.DrawLine(green, W / 2 - W * w / 2, H * top + H * h, W / 2 + W * w / 2, H * top + H * h);
                    g.DrawLine(green, W / 2 - W * w / 2, H * top + H * h, W / 2 - W * w / 2, H * top);
                    g.DrawLine(green, W / 2 + W * w / 2, H * top + H * h, W / 2 + W * w / 2, H * top);
                    g.DrawRectangle(green, W / 2 - 3 * fontWidth / 2, H * top + H * h - fontBottom - fontHeight, 3 * fontWidth, fontHeight);
                    g.DrawLine(green, W / 2 - fontWidth, H * top + H * h - fontBottom, W / 2, H * h + H * top);
                    g.DrawLine(green, W / 2 + fontWidth, H * top + H * h - fontBottom, W / 2, H * h + H * top);
                    g.DrawString(((int)hdg).ToString("000"), f, Brushes.Green, W / 2 - 3 * fontWidth / 2, H * top + H * h - fontBottom - fontHeight);

                    float hudFov = hfov * w;
                    float closestAngle = (float)Math.Round(hdg / 10.0)*10f;
                    float smallestAngle = (float)Math.Floor((closestAngle - hudFov) / 10) * 10f;
                    smallestAngle = ((smallestAngle%360)+360)% 360;
                    float biggestAngle = (float)Math.Ceiling((closestAngle + hudFov) / 10) * 10f;
                    biggestAngle = ((biggestAngle % 360) + 360) % 360;
                    for (float angle = smallestAngle; angle < biggestAngle; angle += 10)
                    {
                        float x = W / 2 + (angle - hdg) / hudFov * W * w;
                        x %= 360 / hfov * W;
                        if (x > W / 2 - fontWidth * 2f && x < W / 2 + fontWidth * 2f)
                        {
                            g.DrawLine(green, x, H * h + H * top, x, H * h + H * top - fontBottom);
                            continue;
                        }
                        if (x < W / 2 - W * w / 2 || x > W / 2 + W * w / 2) continue;
                        if (angle % 90 == 0) g.DrawLine(green, x, H * h + H * top, x, H * h + H * top - bigLine);
                        else g.DrawLine(green, x, H * h + H * top, x, H * h + H * top - smallLine);
                        if (x < W / 2 - W * w / 2 + g.MeasureString(angle.ToString(), f).Width / 2) continue;
                        if (x > W / 2 + W * w / 2 - g.MeasureString(angle.ToString(), f).Width / 2) continue;
                        if (x > W / 2 - fontWidth * 2f - g.MeasureString(angle.ToString(), f).Width / 2 && x < W / 2 + fontWidth * 2f + g.MeasureString(angle.ToString(), f).Width / 2) continue;
                        g.DrawString((Math.Round(((angle%360)+360)%360)).ToString(), f, Brushes.Green, x - g.MeasureString(angle.ToString(), f).Width / 2, H * h + H * top - bigLine * 0.8f - g.MeasureString(angle.ToString(), f).Height);
                    }
                }


                {//ALTITUDE
                    float alt = x/100f-2;
                    float w = 0.1f;
                    float h = 0.7f;
                    float right = 0.01f;
                    float fontHeight = 30;
                    Font f = new Font("Inconsolata", fontHeight * 0.6f, FontStyle.Bold);


                    float anchorX = W - W * w - W * right;
                    float anchorY = H / 2 - H * h / 2;
                    g.DrawRectangle(green, anchorX, anchorY, W * w, H * h);
                    DrawX(anchorX, anchorX);

                    float sizePerMeter = 1f * h;
                    float step = .1f;
                    for(float depth = 0; depth < alt+h/2/sizePerMeter; depth += step)
                    {
                        if (depth < alt - h / 2 / sizePerMeter) continue;
                        float y = H / 2 + H * (depth - alt) * sizePerMeter;
                        g.DrawLine(green, anchorX, y, anchorX + 10, y);
                        g.DrawString(depth.ToString("0.0"), f, Brushes.Green, anchorX + 10, y - g.MeasureString(depth.ToString(), f).Height/2f);

                    }

                    g.DrawRectangle(green, anchorX+9, H / 2 - fontHeight/2, g.MeasureString("0.00", f).Width, fontHeight);
                    g.FillRectangle(Brushes.Black, anchorX+9, H / 2 - fontHeight/2, g.MeasureString("0.00", f).Width, fontHeight);
                    g.DrawString(alt.ToString("0.0"), f, Brushes.Green, anchorX + 9, H / 2 - 12);

                }

                {//LADDER
                    float roll = x - 180;
                    float pitch = y-180;
                    float cos = (float)+Math.Cos(ToRadians(roll));
                    float sin = (float)-Math.Sin(ToRadians(roll));
                    DrawX(W / 2, H / 2);
                    


                    PointF GetRotated(PointF p_) => new PointF(W/2 + p_.X * cos - p_.Y * sin, H/2+p_.X * sin + p_.Y * cos);
                    void DrawLadderStep(float value_, float width_)
                    {
                        g.DrawLine(green, GetRotated(new PointF(-width_/2, value_)), GetRotated(new PointF(width_/2, value_)));
                    }
                }

                {//ROLL
                    float roll = x-180;
                    float bottom = 0.1f;
                    float r = 0.5f - bottom; // vertical
                    float centerX = W / 2;
                    float centerY = H/2;
                    float fontHeight = 30;
                    Font f = new Font("Inconsolata", fontHeight * 0.6f, FontStyle.Bold);

                    float angleVisible = 90;
                    g.DrawArc(green, W / 2 - H * r,H/2- H*r, 2*H*r, 2*H*r, 90-angleVisible/2, angleVisible);
                   
                    float bigLine = 30;
                    float smallLine = bigLine*0.6f;
                    for (float angle = -180; angle < 180; angle += 10)
                    {
                        if ((270 - roll + angle+360)%360 < 270- angleVisible/2) continue;
                        if ((270 - roll + angle+360)%360 > 270+ angleVisible/2) continue;
                        float lineLen = angle % 90 == 0 ? bigLine : smallLine;
                        if(angle < 0)
                        {
                            DrawLineOnArc(centerX, centerY, 270 + roll - angle, H * r + lineLen * 0.0f, H * r + lineLen * 0.2f);
                            DrawLineOnArc(centerX, centerY, 270 + roll - angle, H * r + lineLen * 0.4f, H * r + lineLen * 0.6f);
                            DrawLineOnArc(centerX, centerY, 270 + roll - angle, H * r + lineLen * 0.8f, H * r + lineLen * 1.0f);
                        }
                        else if(angle == 0) DrawLineOnArcBold(centerX, centerY, 270 + roll - angle, H * r- lineLen/4, H * r+ lineLen);
                        else DrawLineOnArc(centerX, centerY, 270 + roll - angle, H * r, H * r+ lineLen);
                        float stringX = W/2+(float)Math.Cos(ToRadians(270 + roll - angle)) * (H * r + fontHeight/2+ bigLine);
                        float stringY = H/2-(float)Math.Sin(ToRadians(270 + roll - angle)) * (H * r+ fontHeight/2 + bigLine);
                        g.DrawString(Math.Abs(angle).ToString(), f, Brushes.Green, stringX - g.MeasureString(Math.Abs(angle).ToString(), f).Width / 2, stringY - g.MeasureString(Math.Abs(angle).ToString(), f).Height / 2);
                        
                    }

                    g.FillRectangle(Brushes.Black, W / 2 - g.MeasureString("-180", f).Width / 2, H - 40, g.MeasureString("-180", f).Width, 30);
                    g.DrawRectangle(green, W / 2 - g.MeasureString("-180", f).Width/2, H - 40, g.MeasureString("-180", f).Width, 30);
                    g.DrawString(roll.ToString(" 000;-000"), f, Brushes.Green, W / 2 - g.MeasureString("-180", f).Width / 2, H - 40);

                }


                double ToRadians(float a_) => (float)(a_*Math.PI/180.0);
                void DrawX(float x, float y) { g.DrawEllipse(Pens.Magenta, x - 4, y - 4, 8, 8); }
                void DrawLineOnArc(float x_, float y_, float a_, float s_, float e_)
                {
                    a_ *= (float)Math.PI / 180.0f;
                    g.DrawLine(green, x_ + (float)(Math.Cos(a_) * s_), y_ - (float)(Math.Sin(a_) * s_), x_ + (float)(Math.Cos(a_) * (e_)), y_ - (float)(Math.Sin(a_) * e_));
                }
                void DrawLineOnArcBold(float x_, float y_, float a_, float s_, float e_)
                {
                    Pen newPen = new Pen(green.Brush, green.Width * 2);
                    newPen.Width = green.Width*2;
                    a_ *= (float)Math.PI / 180.0f;
                    g.DrawLine(newPen, x_ + (float)(Math.Cos(a_) * s_), y_ - (float)(Math.Sin(a_) * s_), x_ + (float)(Math.Cos(a_) * (e_)), y_ - (float)(Math.Sin(a_) * e_));
                }
            }
            Image img = pictureBox1.Image;
            pictureBox1.Image = b;
            if (img != null) img.Dispose();

            // pictureBox1.Invalidate();
            time+=0.1;


            int elapsed = DateTime.Now.Millisecond - start;
            //Text = elapsed.ToString() ;
        }
        int x = 0;
        int y = 0;
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            x = trackBar1.Value;
        }
        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            x = trackBar1.Value;
        }
    }
}
