using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using WiimoteLib;
using WiiTUIO.Filters;
using WiiTUIO.Properties;

namespace WiiTUIO.Provider
{
    public class ScreenPositionCalculator
    {

        private int minXPos;
        private int maxXPos;
        private int maxWidth;

        private int minYPos;
        private int maxYPos;
        private int maxHeight;
        private int SBPositionOffset;
        private double CalcMarginOffsetY;

        private double marginXSlope;
        private double marginYSlope;
        private double minMarginX;
        private double minMarginY;

        private double smoothedX, smoothedZ, smoothedRotation;
        private int orientation;

        private int leftPoint = -1;

        private CursorPos lastPos;

        private Screen primaryScreen;

        private RadiusBuffer smoothingBuffer;
        private CoordFilter coordFilter;

        public ScreenPositionCalculator()
        {
            this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            this.recalculateScreenBounds(this.primaryScreen);

            Settings.Default.PropertyChanged += SettingsChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            lastPos = new CursorPos(0, 0, 0, 0, 0);

            coordFilter = new CoordFilter();
            this.smoothingBuffer = new RadiusBuffer(Settings.Default.pointer_positionSmoothing);
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "primaryMonitor")
            {
                this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
                Console.WriteLine("Setting primary monitor for screen position calculator to " + this.primaryScreen.Bounds);
                this.recalculateScreenBounds(this.primaryScreen);
            }
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            recalculateScreenBounds(this.primaryScreen);
        }

        private void recalculateScreenBounds(Screen screen)
        {
            Console.WriteLine("Setting primary monitor for screen position calculator to " + this.primaryScreen.Bounds);
            minXPos = -(int)(screen.Bounds.Width * Settings.Default.pointer_marginsLeftRight);
            maxXPos = screen.Bounds.Width + (int)(screen.Bounds.Width * Settings.Default.pointer_marginsLeftRight);
            maxWidth = maxXPos - minXPos;
            minYPos = -(int)(screen.Bounds.Height * Settings.Default.pointer_marginsTopBottom);
            maxYPos = screen.Bounds.Height + (int)(screen.Bounds.Height * Settings.Default.pointer_marginsTopBottom);
            maxHeight = maxYPos - minYPos;
            SBPositionOffset = (int)(screen.Bounds.Height * Settings.Default.pointer_sensorBarPosCompensation);
            //CalcMarginOffsetY = 2.8571428571428568 * (0.3 - (Settings.Default.pointer_sensorBarPosCompensation * 0.5));
            CalcMarginOffsetY = Settings.Default.pointer_sensorBarPosCompensation;

            double midMarginX = Settings.Default.pointer_marginsLeftRight * 0.5;
            double midMarginY = Settings.Default.pointer_marginsTopBottom * 0.5;
            marginXSlope = 1.0 / ((1.0 - midMarginX) - midMarginX);
            marginYSlope = 1.0 / ((1.0 - midMarginY) - midMarginY);
            minMarginX = -(marginXSlope * midMarginX);
            minMarginY = -(marginYSlope * midMarginY);
        }

        public CursorPos CalculateCursorPos(WiimoteState wiimoteState)
        {
            int x;
            int y;
            double marginX, marginY = 0.0;

            IRState irState = wiimoteState.IRState;

            PointF relativePosition = new PointF();

            bool foundMidpoint = false;
            for (int i = 0; i < irState.IRSensors.Count() && !foundMidpoint; i++)
            {
                if (irState.IRSensors[i].Found)
                {
                    for (int j = i + 1; j < irState.IRSensors.Count() && !foundMidpoint; j++)
                    {
                        if (irState.IRSensors[j].Found)
                        {
                            foundMidpoint = true;

                            relativePosition.X = (irState.IRSensors[i].Position.X + irState.IRSensors[j].Position.X) / 2.0f;
                            relativePosition.Y = (irState.IRSensors[i].Position.Y + irState.IRSensors[j].Position.Y) / 2.0f;

                            if (Settings.Default.pointer_considerRotation)
                            {
                                smoothedX = smoothedX * 0.9f + wiimoteState.AccelState.RawValues.X * 0.1f;
                                smoothedZ = smoothedZ * 0.9f + wiimoteState.AccelState.RawValues.Z * 0.1f;

                                int l = leftPoint, r;
                                if (leftPoint == -1)
                                {
                                    double absx = Math.Abs(smoothedX - 128), absz = Math.Abs(smoothedZ - 128);

                                    if (orientation == 0 || orientation == 2) absx -= 5;
                                    if (orientation == 1 || orientation == 3) absz -= 5;

                                    if (absz >= absx)
                                    {
                                        if (absz > 5)
                                            orientation = (smoothedZ > 128) ? 0 : 2;
                                    }
                                    else
                                    {
                                        if (absx > 5)
                                            orientation = (smoothedX > 128) ? 3 : 1;
                                    }

                                    switch (orientation)
                                    {
                                        case 0: l = (irState.IRSensors[i].RawPosition.X < irState.IRSensors[j].RawPosition.X) ? i : j; break;
                                        case 1: l = (irState.IRSensors[i].RawPosition.Y > irState.IRSensors[j].RawPosition.Y) ? i : j; break;
                                        case 2: l = (irState.IRSensors[i].RawPosition.X > irState.IRSensors[j].RawPosition.X) ? i : j; break;
                                        case 3: l = (irState.IRSensors[i].RawPosition.Y < irState.IRSensors[j].RawPosition.Y) ? i : j; break;
                                    }
                                }
                                leftPoint = l;
                                r = l == i ? j : i;

                                double dx = irState.IRSensors[r].RawPosition.X - irState.IRSensors[l].RawPosition.X;
                                double dy = irState.IRSensors[r].RawPosition.Y - irState.IRSensors[l].RawPosition.Y;

                                double d = Math.Sqrt(dx * dx + dy * dy);

                                dx /= d;
                                dy /= d;

                                smoothedRotation = Math.Atan2(dy, dx);
                            }
                        }
                    }
                }
            }

            if (!foundMidpoint)
            {
                CursorPos err = lastPos;
                err.OutOfReach = true;
                leftPoint = -1;

                return err;
            }

            int offsetY = 0;
            double marginOffsetY = 0.0;

            if (Properties.Settings.Default.pointer_sensorBarPos == "top")
            {
                offsetY = -SBPositionOffset;
                marginOffsetY = -CalcMarginOffsetY;
            }
            else if (Properties.Settings.Default.pointer_sensorBarPos == "bottom")
            {
                offsetY = SBPositionOffset;
                marginOffsetY = CalcMarginOffsetY;
            }

            relativePosition.X = 1 - relativePosition.X;

            if (Settings.Default.pointer_considerRotation)
            {
                relativePosition.X = relativePosition.X - 0.5F;
                relativePosition.Y = relativePosition.Y - 0.5F;

                relativePosition = this.rotatePoint(relativePosition, smoothedRotation);

                relativePosition.X = relativePosition.X + 0.5F;
                relativePosition.Y = relativePosition.Y + 0.5F;
            }

            /*System.Windows.Point filteredPoint = coordFilter.AddGetFilteredCoord(new System.Windows.Point(relativePosition.X, relativePosition.Y), 1.0, 1.0);
            
            relativePosition.X = (float)filteredPoint.X;
            relativePosition.Y = (float)filteredPoint.Y;

            Vector smoothedPoint = smoothingBuffer.AddAndGet(new Vector(relativePosition.X, relativePosition.Y));
            */


            //x = Convert.ToInt32((float)maxWidth * smoothedPoint.X + minXPos);
            //y = Convert.ToInt32((float)maxHeight * smoothedPoint.Y + minYPos) + offsetY;
            x = Convert.ToInt32((float)maxWidth * relativePosition.X + minXPos);
            y = Convert.ToInt32((float)maxHeight * relativePosition.Y + minYPos) + offsetY;
            //x = Convert.ToInt32((float)3902 * relativePosition.X + (-1170)); // input: [0.3, 0.65]
            //y = Convert.ToInt32((float)2191 * relativePosition.Y + (-657)) + offsetY; // Input: [0.3, 0.65]

            //// input: [0.3, 0.65]
            //marginX = Math.Min(1.0, Math.Max(0.0, 2.8571428571428568 * relativePosition.X - 0.857142857142857));
            //// input: [0.3, 0.65]
            //marginY = Math.Min(1.0, Math.Max(0.0, 2.8571428571428568 * relativePosition.Y + (marginOffsetY - 0.857142857142857)));
            marginX = Math.Min(1.0, Math.Max(0.0, marginXSlope * relativePosition.X + minMarginX));
            marginY = Math.Min(1.0, Math.Max(0.0, marginYSlope * relativePosition.Y + (marginOffsetY + minMarginY)));

            //System.Diagnostics.Trace.WriteLine($"{marginY} | {relativePosition.Y}");

            if (x <= 0)
            {
                x = 0;
            }
            else if (x >= primaryScreen.Bounds.Width)
            {
                x = primaryScreen.Bounds.Width - 1;
            }
            if (y <= 0)
            {
                y = 0;
            }
            else if (y >= primaryScreen.Bounds.Height)
            {
                y = primaryScreen.Bounds.Height - 1;
            }

            //Console.WriteLine("{0} {1} {2}", relativePosition.X, marginX, x / (double)primaryScreen.Bounds.Width);

            //CursorPos result = new CursorPos(x, y, smoothedPoint.X, smoothedPoint.Y, smoothedRotation);
            CursorPos result = new CursorPos(x, y, relativePosition.X, relativePosition.Y, smoothedRotation,
                marginX, marginY);
            lastPos = result;
            return result;
        }

        private PointF rotatePoint(PointF point, double angle)
        {
            double sin = Math.Sin(angle);
            double cos = Math.Cos(angle);

            double xnew = point.X * cos - point.Y * sin;
            double ynew = point.X * sin + point.Y * cos;

            PointF result;

            result.X = (float)xnew;
            result.Y = (float)ynew;

            return result;
        }

    }
}
