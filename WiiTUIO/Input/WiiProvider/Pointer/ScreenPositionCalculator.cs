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
using Point = WiimoteLib.Point;

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

        private PointF topLeftPt = new PointF();
        private PointF centerPt = new PointF();
        private double lightbarXSlope;
        private double lightbarYSlope;
        private double lightbarXIntercept;
        private double lightbarYIntercept;

        private double smoothedX, smoothedZ, smoothedRotation;
        private int orientation;

        private int leftPoint = -1;

        private CursorPos lastPos;

        private Screen primaryScreen;

        private RadiusBuffer smoothingBuffer;
        private CoordFilter coordFilter;

        private int lastIrPoint1 = -1;
        private int lastIrPoint2 = -1;

        public ScreenPositionCalculator()
        {
            this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
            this.recalculateScreenBounds(this.primaryScreen);

            Settings.Default.PropertyChanged += SettingsChanged;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            lastPos = new CursorPos(0, 0, 0, 0, 0);

            coordFilter = new CoordFilter();
            this.smoothingBuffer = new RadiusBuffer(Settings.Default.pointer_positionSmoothing);

            //topLeftPt = new PointF() { X = 0.0f, Y = 0.0f };
            //centerPt = new PointF() { X = 0.5f, Y = 0.5f };
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "primaryMonitor")
            {
                this.primaryScreen = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor);
                Console.WriteLine("Setting primary monitor for screen position calculator to " + this.primaryScreen.Bounds);
                this.recalculateScreenBounds(this.primaryScreen);
            }
            else
            {
                switch (e.PropertyName)
                {
                    case "test_topLeftGunX":
                        topLeftPt.X = (float)Settings.Default.test_topLeftGunX;
                        break;
                    case "test_topLeftGunY":
                        topLeftPt.Y = (float)Settings.Default.test_topLeftGunY;
                        break;
                    case "test_centerGunX":
                        centerPt.X = (float)Settings.Default.test_centerGunX;
                        break;
                    case "test_centerGunY":
                        centerPt.Y = (float)Settings.Default.test_centerGunY;
                        break;
                    default: break;
                }
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

            //topLeftPt = new PointF() { X = 0.76f, Y = 0.02f };
            //centerPt = new PointF() { X = 0.48f, Y = 0.25f };
            //topLeftPt = new PointF() { X = 0.22f, Y = 0.02f };
            //centerPt = new PointF() { X = 0.50f, Y = 0.40f };

            // OLD WORKING
            //topLeftPt = new PointF() { X = 0.22f, Y = 0.02f };
            //centerPt = new PointF() { X = 0.46f, Y = 0.17f };

            // NEWER WORKING
            //topLeftPt = new PointF() { X = 0.18f, Y = 0.01f };
            //centerPt = new PointF() { X = 0.50f, Y = 0.19f };
            //topLeftPt = new PointF() { X = 0.166f, Y = 0.004f };
            //centerPt = new PointF() { X = 0.44f, Y = 0.19f };

            //topLeftPt = new PointF() { X = 0.15f, Y = 0.002f };
            //centerPt = new PointF() { X = 0.43f, Y = 0.205f };

            topLeftPt = new PointF() { X = (float)Settings.Default.test_topLeftGunX,
                Y = (float)Settings.Default.test_topLeftGunY
            };
            centerPt = new PointF() { X = (float)Settings.Default.test_centerGunX,
                Y = (float)Settings.Default.test_centerGunY
            };

            // topLeftPt = new PointF() { X = 0.21f, Y = 0.01f };
            // centerPt = new PointF() { X = 0.46f, Y = 0.17f };

            //topLeftPt = new PointF() { X = 0.17928672f, Y = 0.00781759f };
            //centerPt = new PointF() { X = 0.4391793f, Y = 0.14462541f };

            //lightbarXSlope = ((topLeftPt.X - centerPt.X) * 2.0) / (0.8 - 0.2);
            //lightbarYSlope = ((centerPt.Y - topLeftPt.Y) * 2.0) / (0.8 - 0.2);
            lightbarXSlope = 1.0 / ((centerPt.X - topLeftPt.X) * 2.0);
            lightbarYSlope = 1.0 / ((centerPt.Y - topLeftPt.Y) * 2.0);
            lightbarXIntercept = 1.0 - (lightbarXSlope * (centerPt.X + (centerPt.X - topLeftPt.X)));
            lightbarYIntercept = 1.0 - (lightbarYSlope * (centerPt.Y + (centerPt.Y - topLeftPt.Y)));
        }

        public CursorPos CalculateCursorPos(WiimoteState wiimoteState)
        {
            int x;
            int y;
            double marginX, marginY = 0.0;
            double lightbarX = 0.0;
            double lightbarY = 0.0;

            IRState irState = wiimoteState.IRState;

            PointF relativePosition = new PointF();

            int irPoint1 = 0;
            int irPoint2 = 0;
            bool foundMidpoint = false;
            // First check if previously found points are still detected.
            // Prefer those points first
            if (lastIrPoint1 != -1 && lastIrPoint2 != -1)
            {
                if (irState.IRSensors[lastIrPoint1].Found &&
                    irState.IRSensors[lastIrPoint2].Found)
                {
                    foundMidpoint = true;
                    irPoint1 = lastIrPoint1;
                    irPoint2 = lastIrPoint2;
                }
            }

            // If no midpoint found from previous points, check all available
            // IR points for a possible midpoint
            for (int i = 0; !foundMidpoint && i < irState.IRSensors.Count(); i++)
            {
                if (irState.IRSensors[i].Found)
                {
                    for (int j = i + 1; j < irState.IRSensors.Count() && !foundMidpoint; j++)
                    {
                        if (irState.IRSensors[j].Found)
                        {
                            foundMidpoint = true;

                            irPoint1 = i;
                            irPoint2 = j;
                        }
                    }
                }
            }

            if (foundMidpoint)
            {
                int i = irPoint1;
                int j = irPoint2;
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

                lastIrPoint1 = irPoint1;
                lastIrPoint2 = irPoint2;
            }
            else if (!foundMidpoint)
            {
                CursorPos err = lastPos;
                err.OutOfReach = true;
                leftPoint = -1;
                lastIrPoint1 = -1;
                lastIrPoint2 = -1;

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

            lightbarX = Math.Min(1.0,
                Math.Max(0.0, lightbarXSlope * relativePosition.X + lightbarXIntercept));
            lightbarY = Math.Min(1.0,
                Math.Max(0.0, lightbarYSlope * relativePosition.Y + lightbarYIntercept));

            //System.Diagnostics.Trace.WriteLine($"X {lightbarX} | {relativePosition.X}");
            //System.Diagnostics.Trace.WriteLine($"Y {lightbarY} | {relativePosition.Y}");

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
                marginX, marginY, lightbarX, lightbarY);
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

            xnew = Math.Min(0.5, Math.Max(-0.5, xnew));
            ynew = Math.Min(0.5, Math.Max(-0.5, ynew));

            result.X = (float)xnew;
            result.Y = (float)ynew;

            return result;
        }

    }
}
