﻿using System;
using System.Collections.Generic;
using System.Drawing;

namespace KNRAnglerN
{
    class HUD
    {
        public int width, height;
        public float vFov, hFov;
        public Gyro gyro;
        public float metersUnderWater;
        public float batteryFill = 0.9f, batteryVoltage = 10.1f;
        private Dictionary<string, bool> enable = new Dictionary<string, bool>();

        public HUD(int width, int height, float vFov, float hFov)
        {
            this.width = width;
            this.height = height;
            this.vFov = vFov;
            this.hFov = hFov;
            enable.Add("heading", true);
            enable.Add("ladder", true);
            enable.Add("altitude", true);
            enable.Add("roll", true);
            enable.Add("battery", true);
        }

        public void Enable(string name, bool state)
        {
            if (enable.ContainsKey(name))
                enable[name] = state;
        }

        public void Enable(string[] names, bool state)
        {
            foreach (var name in names)
                if (enable.ContainsKey(name))
                    enable[name] = state;
        }

        public void EnableAll(bool state)
        {
            foreach (var key in enable.Keys)
                enable[key] = state;
        }

        public void Update(Gyro gyro)
        {
            this.gyro = gyro;
        }

        public Bitmap Generate()
        {
            int W = width;
            int H = height;
            Bitmap b = new Bitmap(W, H);
            using (Graphics g = Graphics.FromImage(b))
            {
                g.FillRectangle(Brushes.Black, 0, 0, W, H);

                using Pen green = new Pen(Brushes.Red, 3);
                using Pen debug = new Pen(Brushes.Aquamarine, 1);

                if (enable["ladder"])
                {//LADDER
                    float w = 0.5f;
                    float wLadder = 0.5f;
                    float wHorizont = 1.5f;
                    float roll = gyro.z;
                    float pitch = gyro.x;
                    float cos = (float)+Math.Cos(ToRadians(-roll));
                    float sin = (float)-Math.Sin(ToRadians(-roll));
                    float fontHeight = 20;
                    using Font f = new Font("Inconsolata", fontHeight * 0.6f, FontStyle.Bold);

                    float ellSize = 0.01f;
                    g.DrawEllipse(green, W / 2 - W * ellSize / 2, H / 2 - W * ellSize / 2, W * ellSize, W * ellSize);
                    g.DrawLine(green, GetRotated(new PointF(-W * ellSize * 2, 0)), GetRotated(new PointF(-W * ellSize / 2, 0)));
                    g.DrawLine(green, GetRotated(new PointF(W * ellSize * 2, 0)), GetRotated(new PointF(W * ellSize / 2, 0)));
                    for (float angle = -90; angle <= 90; angle += 10)
                    {
                        float y = (angle + pitch) / vFov * H;
                        if (y > H / 2 + 180f / vFov * H) y -= 360f / vFov * H;
                        if (y < H / 2 - 180f / vFov * H) y += 360f / vFov * H;
                        if (angle == 0) DrawHorizont(y, W * w * wHorizont, 0.1f);
                        else
                        {
                            if (angle % 90 == 0) DrawLadderStep(y, W * w * wLadder * 1.1f, 0.4f, angle);
                            else
                            {
                                if (angle > 0) DrawLadderStep(y, W * w * wLadder, 0.4f, angle);
                                else DrawNegativeLadderStep(y, W * w * wLadder, 0.4f, angle);
                            }
                        }
                    }

                    PointF GetRotated(PointF p_) => new PointF(W / 2 + p_.X * cos + p_.Y * sin, H / 2 + p_.X * sin - p_.Y * cos);
                    void DrawLadderStep(float value_, float width_, float hole, float angle)
                    {
                        g.DrawLine(green, GetRotated(new PointF(-width_ / 2, value_)), GetRotated(new PointF(-width_ * hole / 2, value_)));
                        g.DrawLine(green, GetRotated(new PointF(width_ * hole / 2, value_)), GetRotated(new PointF(width_ / 2, value_)));
                        g.DrawLine(green, GetRotated(new PointF(-width_ * hole / 2, value_ - 15)), GetRotated(new PointF(-width_ * hole / 2, value_)));
                        g.DrawLine(green, GetRotated(new PointF(width_ * hole / 2, value_ - 15)), GetRotated(new PointF(width_ * hole / 2, value_)));

                        var textRight = GetRotated(new PointF(width_ / 2 + g.MeasureString("00", f).Width / 2, value_));
                        g.DrawString(angle.ToString(), f, Brushes.Red, textRight.X - g.MeasureString("00", f).Width / 2, textRight.Y - g.MeasureString("00", f).Height / 2);
                        var textLeft = GetRotated(new PointF(-(width_ / 2 + g.MeasureString("00", f).Width / 2), value_));
                        g.DrawString(angle.ToString(), f, Brushes.Red, textLeft.X - g.MeasureString("00", f).Width / 2, textLeft.Y - g.MeasureString("00", f).Height / 2);
                    }

                    void DrawHorizont(float value_, float width_, float hole)
                    {
                        g.DrawLine(green, GetRotated(new PointF(-width_ / 2, value_)), GetRotated(new PointF(-width_ * hole / 2, value_)));
                        g.DrawLine(green, GetRotated(new PointF(width_ * hole / 2, value_)), GetRotated(new PointF(width_ / 2, value_)));
                    }

                    void DrawNegativeLadderStep(float value_, float width_, float hole, float angle)
                    {
                        angle = angle > 0 ? angle : -angle;
                        float line = (width_ - width_ * hole) / 2;
                        float max = width_ / 2;
                        g.DrawLine(green, GetRotated(new PointF(max - line * 0.2f, value_)), GetRotated(new PointF(max, value_)));
                        g.DrawLine(green, GetRotated(new PointF(max - line * 0.6f, value_)), GetRotated(new PointF(max - line * 0.4f, value_)));
                        g.DrawLine(green, GetRotated(new PointF(max - line, value_)), GetRotated(new PointF(max - line * 0.8f, value_)));
                        g.DrawLine(green, GetRotated(new PointF(-max + line * 0.2f, value_)), GetRotated(new PointF(-max, value_)));
                        g.DrawLine(green, GetRotated(new PointF(-max + line * 0.6f, value_)), GetRotated(new PointF(-max + line * 0.4f, value_)));
                        g.DrawLine(green, GetRotated(new PointF(-max + line, value_)), GetRotated(new PointF(-max + line * 0.8f, value_)));

                        g.DrawLine(green, GetRotated(new PointF(-width_ * hole / 2, value_ + 15)), GetRotated(new PointF(-width_ * hole / 2, value_)));
                        g.DrawLine(green, GetRotated(new PointF(width_ * hole / 2, value_ + 15)), GetRotated(new PointF(width_ * hole / 2, value_)));

                        var textRight = GetRotated(new PointF(width_ / 2 + g.MeasureString("00", f).Width / 2, value_));
                        g.DrawString(angle.ToString(), f, Brushes.Red, textRight.X - g.MeasureString("00", f).Width / 2, textRight.Y - g.MeasureString("00", f).Height / 2);
                        var textLeft = GetRotated(new PointF(-(width_ / 2 + g.MeasureString("00", f).Width / 2), value_));
                        g.DrawString(angle.ToString(), f, Brushes.Red, textLeft.X - g.MeasureString("00", f).Width / 2, textLeft.Y - g.MeasureString("00", f).Height / 2);

                    }
                }

                if (enable["heading"])
                {//HEADING\
                    float w = 0.5f;
                    float h = 0.1f;
                    float top = 0.05f;

                    float anchorX = W / 2 - W * w / 2;
                    float anchorY = H * top;

                    g.FillRectangle(Brushes.Black, anchorX, anchorY, W * w, H * h);
                    //g.DrawRectangle(debug, anchorX, anchorY, W * w, H * h);
                    g.DrawLine(green, anchorX, anchorY + H * h, anchorX + W * w, anchorY + H * h);

                    float textBoxH = 0.7f;
                    float fontHeight = H * h * textBoxH;
                    using Font f = new Font("Inconsolata", fontHeight * 0.7f, FontStyle.Bold);

                    float rulerH = 0.3f;
                    float fontSmallH = 0.8f;
                    float fontSmallHeight = H * h * (1f - rulerH) * fontSmallH;
                    Font fsmall = new Font("Inconsolata", fontSmallHeight * 0.7f, FontStyle.Bold);

                    float hdg = ((gyro.y % 360f) + 360f) % 360f;

                    float lineY = anchorY + H * h;
                    float smallLine = 0.7f;
                    float bigLine = 1f;
                    for (float angle = 0f; angle < 360; angle += 5)
                    {
                        float x = W / 2 + (angle - hdg) / hFov * W;
                        if (x > W / 2 + 180f / hFov * W) x -= 360f / hFov * W;
                        if (x < W / 2 - 180f / hFov * W) x += 360f / hFov * W;
                        if (x < anchorX || x > anchorX + W * w) continue;
                        string str = angle.ToString();
                        float lineH = smallLine / 2;
                        if (angle % 10 == 0) lineH = smallLine;
                        if (angle % 90 == 0) lineH = bigLine;

                        if (angle % 10 != 0) str = "";
                        if (angle == 0) str = "N";
                        if (angle == 45) str = "NE";
                        if (angle == 90) str = "E";
                        if (angle == 135) str = "SE";
                        if (angle == 180) str = "S";
                        if (angle == 225) str = "SW";
                        if (angle == 270) str = "W";
                        if (angle == 315) str = "NW";

                        g.DrawLine(green, x, lineY, x, lineY - H * h * rulerH * lineH);
                        g.DrawString(str, fsmall, Brushes.Red, x - g.MeasureString(str, fsmall).Width / 2, lineY - H * h * rulerH * lineH - g.MeasureString(str, fsmall).Height);
                    }


                    float fontWidth = g.MeasureString("000", f).Width / 3;
                    float textBoxW = 0.1f;
                    float textBoxX = W / 2 - fontWidth * 3 / 2;
                    float textBoxY = anchorY;
                    g.DrawLine(green, W / 2, lineY, W / 2 - fontWidth * 3 / 4, lineY - H * h + fontHeight);
                    g.DrawLine(green, W / 2, lineY, W / 2 + fontWidth * 3 / 4, lineY - H * h + fontHeight);
                    g.FillRectangle(Brushes.Black, textBoxX, textBoxY, fontWidth * 3, fontHeight);
                    g.DrawRectangle(green, textBoxX, textBoxY, fontWidth * 3, fontHeight);
                    g.DrawString(((int)hdg).ToString("000"), f, Brushes.Red, textBoxX, textBoxY);

                }

                if (enable["altitude"])
                {//ALTITUDE
                    float alt = metersUnderWater;
                    float w = 0.1f;
                    float h = 0.7f;
                    float right = 0.01f;
                    float fontHeight = 30;
                    using Font f = new Font("Inconsolata", fontHeight * 0.6f, FontStyle.Bold);


                    float anchorX = W - W * w - W * right;
                    float anchorY = H / 2 - H * h / 2;
                    g.FillRectangle(Brushes.Black, anchorX, anchorY, W * w, H * h);
                    //g.DrawRectangle(green, anchorX, anchorY, W * w, H * h);
                    g.DrawLine(green, anchorX, anchorY, anchorX, anchorY + H * h);
                    DrawX(anchorX, anchorX);

                    float sizePerMeter = 1f * h;
                    float step = .1f;
                    for (float depth = 0; depth < alt + h / 2 / sizePerMeter; depth += step)
                    {
                        if (depth < alt - h / 2 / sizePerMeter) continue;
                        float y = H / 2 + H * (depth - alt) * sizePerMeter;
                        g.DrawLine(green, anchorX, y, anchorX + 10, y);
                        g.DrawString(depth.ToString("0.0"), f, Brushes.Red, anchorX + 10, y - g.MeasureString(depth.ToString(), f).Height / 2f);

                    }

                    g.DrawLine(green, anchorX, anchorY + H * h / 2, anchorX + 9, anchorY + H * h / 2 + fontHeight / 4);
                    g.DrawLine(green, anchorX, anchorY + H * h / 2, anchorX + 9, anchorY + H * h / 2 - fontHeight / 4);
                    g.FillRectangle(Brushes.Black, anchorX + 9, H / 2 - fontHeight / 2, g.MeasureString("0.00", f).Width, fontHeight);
                    g.DrawRectangle(green, anchorX + 9, H / 2 - fontHeight / 2, g.MeasureString("0.00", f).Width, fontHeight);
                    g.DrawString(alt.ToString("0.0"), f, Brushes.Red, anchorX + 9, H / 2 - 12);

                }

                if (enable["roll"])
                {//ROLL
                    float roll = gyro.z;
                    float bottom = 0.1f;
                    float r = 0.5f - bottom; // vertical
                    float centerX = W / 2;
                    float centerY = H / 2;
                    float fontHeight = 30;
                    using Font f = new Font("Inconsolata", fontHeight * 0.6f, FontStyle.Bold);

                    float angleVisible = 90;
                    g.DrawArc(green, W / 2 - H * r, H / 2 - H * r, 2 * H * r, 2 * H * r, 90 - angleVisible / 2, angleVisible);

                    float bigLine = 30;
                    float smallLine = bigLine * 0.6f;
                    for (float angle = -180; angle < 180; angle += 10)
                    {
                        if ((270 - roll + angle + 360) % 360 < 270 - angleVisible / 2) continue;
                        if ((270 - roll + angle + 360) % 360 > 270 + angleVisible / 2) continue;
                        float lineLen = angle % 90 == 0 ? bigLine : smallLine;
                        if (angle < 0)
                        {
                            DrawLineOnArc(centerX, centerY, 270 + roll - angle, H * r + lineLen * 0.0f, H * r + lineLen * 0.2f);
                            DrawLineOnArc(centerX, centerY, 270 + roll - angle, H * r + lineLen * 0.4f, H * r + lineLen * 0.6f);
                            DrawLineOnArc(centerX, centerY, 270 + roll - angle, H * r + lineLen * 0.8f, H * r + lineLen * 1.0f);
                        }
                        else if (angle == 0) DrawLineOnArcBold(centerX, centerY, 270 + roll - angle, H * r - lineLen / 4, H * r + lineLen);
                        else DrawLineOnArc(centerX, centerY, 270 + roll - angle, H * r, H * r + lineLen);
                        float stringX = W / 2 + (float)Math.Cos(ToRadians(270 + roll - angle)) * (H * r + fontHeight / 2 + bigLine);
                        float stringY = H / 2 - (float)Math.Sin(ToRadians(270 + roll - angle)) * (H * r + fontHeight / 2 + bigLine);
                        g.DrawString(Math.Abs(angle).ToString(), f, Brushes.Red, stringX - g.MeasureString(Math.Abs(angle).ToString(), f).Width / 2, stringY - g.MeasureString(Math.Abs(angle).ToString(), f).Height / 2);

                    }
                    g.FillRectangle(Brushes.Black, W / 2 - g.MeasureString("-180", f).Width / 2, H - 40, g.MeasureString("-180", f).Width, 30);
                    g.DrawRectangle(green, W / 2 - g.MeasureString("-180", f).Width / 2, H - 40, g.MeasureString("-180", f).Width, 30);
                    if (roll > 180) roll -= 360f;
                    g.DrawString(roll.ToString(" 000;-000"), f, Brushes.Red, W / 2 - g.MeasureString("-180", f).Width / 2, H - 40);

                }

                if (enable["battery"])
                {//BATTERY
                    float w = 0.2f;
                    float h = 0.05f;
                    float x = 0.01f;
                    float y = 0.2f;
                    float anchorX = W * x;
                    float anchorY = H * y;

                    g.FillRectangle(Brushes.Black, anchorX, anchorY, W * w, H * h);
                    g.DrawRectangle(green, anchorX, anchorY, W * w, H * h);
                    g.FillRectangle(Brushes.Red, anchorX, anchorY, W * w * batteryFill, H * h);

                    float textBoxH = 0.05f;
                    float fontHeight = H * textBoxH;
                    using Font f = new Font("Inconsolata", fontHeight * 0.7f, FontStyle.Bold);
                    g.DrawString("BAT " + batteryVoltage.ToString(".0"), f, Brushes.Red, anchorX, anchorY - g.MeasureString("-180", f).Height);
                }


                double ToRadians(float a_) => (float)(a_ * Math.PI / 180.0);
                void DrawX(float x, float y) { g.DrawEllipse(Pens.Magenta, x - 4, y - 4, 8, 8); }
                void DrawLineOnArc(float x_, float y_, float a_, float s_, float e_)
                {
                    a_ *= (float)Math.PI / 180.0f;
                    g.DrawLine(green, x_ + (float)(Math.Cos(a_) * s_), y_ - (float)(Math.Sin(a_) * s_), x_ + (float)(Math.Cos(a_) * (e_)), y_ - (float)(Math.Sin(a_) * e_));
                }
                void DrawLineOnArcBold(float x_, float y_, float a_, float s_, float e_)
                {
                    using Pen newPen = new Pen(green.Brush, green.Width * 2);
                    newPen.Width = green.Width * 2;
                    a_ *= (float)Math.PI / 180.0f;
                    g.DrawLine(newPen, x_ + (float)(Math.Cos(a_) * s_), y_ - (float)(Math.Sin(a_) * s_), x_ + (float)(Math.Cos(a_) * (e_)), y_ - (float)(Math.Sin(a_) * e_));
                }
            }

            b.MakeTransparent();
            return b;
        }
    }
}
