using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiTUIO.Provider;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using System.Windows;

namespace WiiTUIO.Output.Handlers.Xinput
{
    public class ViGEmHandler : IButtonHandler, IStickHandler, IRumbleFeedback, ICursorHandler
    {
        private const string PREFIX = "360.";

        private ViGEmBusClient viGEmClient;
        private ViGEmBus360Device device;
        private long id;
        private CursorPositionHelper cursorPositionHelper;

        public Action<byte, byte> OnRumble { get; set; }

        public ViGEmHandler(long id)
        {
            this.id = id;
            viGEmClient = ViGEmBusClientSingleton.Default;
            cursorPositionHelper = new CursorPositionHelper();
            device = new ViGEmBus360Device(viGEmClient.VigemTestClient);
            device.OnRumble += Device_OnRumble;
        }

        private void Device_OnRumble(byte arg1, byte arg2)
        {
            OnRumble?.Invoke(arg1, arg2);
        }

        public bool connect()
        {
            if (viGEmClient.VigemTestClient != null)
            {
                device.Connect();
                return true;
            }

            return false;
        }

        public bool disconnect()
        {
            if (viGEmClient.VigemTestClient != null)
            {
                device.Disconnect();
                return true;
            }

            return false;
        }

        public bool setButtonDown(string key)
        {
            if (key.Length > 4 && key.ToLower().Substring(0, 4).Equals(PREFIX))
            {
                string button = key.ToLower().Substring(4);
                switch (button)
                {
                    case "triggerr":
                        device.report.RightTrigger = 255;
                        break;
                    case "triggerl":
                        device.report.LeftTrigger = 255;
                        break;
                    case "a":
                        device.report.SetButtonState(Xbox360Buttons.A, true);
                        break;
                    case "b":
                        device.report.SetButtonState(Xbox360Buttons.B, true);
                        break;
                    case "x":
                        device.report.SetButtonState(Xbox360Buttons.X, true);
                        break;
                    case "y":
                        device.report.SetButtonState(Xbox360Buttons.Y, true);
                        break;
                    case "back":
                        device.report.SetButtonState(Xbox360Buttons.Back, true);
                        break;
                    case "start":
                        device.report.SetButtonState(Xbox360Buttons.Start, true);
                        break;
                    case "stickpressl":
                        device.report.SetButtonState(Xbox360Buttons.LeftThumb, true);
                        break;
                    case "stickpressr":
                        device.report.SetButtonState(Xbox360Buttons.RightThumb, true);
                        break;
                    case "up":
                        device.report.SetButtonState(Xbox360Buttons.Up, true);
                        break;
                    case "down":
                        device.report.SetButtonState(Xbox360Buttons.Down, true);
                        break;
                    case "right":
                        device.report.SetButtonState(Xbox360Buttons.Right, true);
                        break;
                    case "left":
                        device.report.SetButtonState(Xbox360Buttons.Left, true);
                        break;
                    case "guide":
                        device.report.SetButtonState(Xbox360Buttons.Guide, true);
                        break;
                    case "bumperl":
                        device.report.SetButtonState(Xbox360Buttons.LeftShoulder, true);
                        break;
                    case "bumperr":
                        device.report.SetButtonState(Xbox360Buttons.RightShoulder, true);
                        break;
                    case "stickrright":
                        device.report.SetAxis(Xbox360Axes.RightThumbX, 32767);
                        break;
                    case "stickrup":
                        device.report.SetAxis(Xbox360Axes.RightThumbY, 32767);
                        break;
                    case "sticklright":
                        device.report.SetAxis(Xbox360Axes.LeftThumbX, 32767);
                        break;
                    case "sticklup":
                        device.report.SetAxis(Xbox360Axes.LeftThumbY, 32767);
                        break;
                    case "stickrleft":
                        device.report.SetAxis(Xbox360Axes.RightThumbX, -32768);
                        break;
                    case "stickrdown":
                        device.report.SetAxis(Xbox360Axes.RightThumbY, -32768);
                        break;
                    case "sticklleft":
                        device.report.SetAxis(Xbox360Axes.LeftThumbX, -32768);
                        break;
                    case "stickldown":
                        device.report.SetAxis(Xbox360Axes.LeftThumbY, -32768);
                        break;
                    default:
                        return false; //No valid key code was found
                }
                return true;
            }
            return false;
        }

        public bool setButtonUp(string key)
        {
            if (key.Length > 4 && key.ToLower().Substring(0, 4).Equals(PREFIX))
            {
                string button = key.ToLower().Substring(4);
                switch (button)
                {
                    case "triggerr":
                        device.report.RightTrigger = 0;
                        break;
                    case "triggerl":
                        device.report.LeftTrigger = 0;
                        break;
                    case "a":
                        device.report.SetButtonState(Xbox360Buttons.A, false);
                        break;
                    case "b":
                        device.report.SetButtonState(Xbox360Buttons.B, false);
                        break;
                    case "x":
                        device.report.SetButtonState(Xbox360Buttons.X, false);
                        break;
                    case "y":
                        device.report.SetButtonState(Xbox360Buttons.Y, false);
                        break;
                    case "back":
                        device.report.SetButtonState(Xbox360Buttons.Back, false);
                        break;
                    case "start":
                        device.report.SetButtonState(Xbox360Buttons.Start, false);
                        break;
                    case "stickpressl":
                        device.report.SetButtonState(Xbox360Buttons.LeftThumb, false);
                        break;
                    case "stickpressr":
                        device.report.SetButtonState(Xbox360Buttons.RightThumb, false);
                        break;
                    case "up":
                        device.report.SetButtonState(Xbox360Buttons.Up, false);
                        break;
                    case "down":
                        device.report.SetButtonState(Xbox360Buttons.Down, false);
                        break;
                    case "right":
                        device.report.SetButtonState(Xbox360Buttons.Right, false);
                        break;
                    case "left":
                        device.report.SetButtonState(Xbox360Buttons.Left, false);
                        break;
                    case "guide":
                        device.report.SetButtonState(Xbox360Buttons.Guide, false);
                        break;
                    case "bumperl":
                        device.report.SetButtonState(Xbox360Buttons.LeftShoulder, false);
                        break;
                    case "bumperr":
                        device.report.SetButtonState(Xbox360Buttons.RightShoulder, false);
                        break;
                    case "stickrright":
                        device.report.SetAxis(Xbox360Axes.RightThumbX, 0);
                        break;
                    case "stickrup":
                        device.report.SetAxis(Xbox360Axes.RightThumbY, 0);
                        break;
                    case "sticklright":
                        device.report.SetAxis(Xbox360Axes.LeftThumbX, 0);
                        break;
                    case "sticklup":
                        device.report.SetAxis(Xbox360Axes.LeftThumbY, 0);
                        break;
                    case "stickrleft":
                        device.report.SetAxis(Xbox360Axes.RightThumbX, 0);
                        break;
                    case "stickrdown":
                        device.report.SetAxis(Xbox360Axes.RightThumbY, 0);
                        break;
                    case "sticklleft":
                        device.report.SetAxis(Xbox360Axes.LeftThumbX, 0);
                        break;
                    case "stickldown":
                        device.report.SetAxis(Xbox360Axes.LeftThumbY, 0);
                        break;
                    default:
                        return false; //No valid key code was found
                }
                return true;
            }
            return false;
        }

        public bool setPosition(string key, CursorPos cursorPos)
        {
            key = key.ToLower();
            if (key.Equals("360.stickl") || key.Equals("360.stickr"))
            {
                if (!cursorPos.OutOfReach)
                {
                    Point smoothedPos = cursorPositionHelper.getSmoothedPosition(new Point(cursorPos.RelativeX, cursorPos.RelativeY));

                    double smoothedX = smoothedPos.X;
                    //double smoothedY = 1 - smoothedPos.Y; // Y is inverted
                    double smoothedY = smoothedPos.Y;

                    switch (key)
                    {
                        case "360.stickl":
                            device.report.SetAxis(Xbox360Axes.LeftThumbX, AxisScale(smoothedX, false));
                            device.report.SetAxis(Xbox360Axes.LeftThumbY, AxisScale(smoothedY, true));
                            break;
                        case "360.stickr":
                            device.report.SetAxis(Xbox360Axes.RightThumbX, AxisScale(smoothedX, false));
                            device.report.SetAxis(Xbox360Axes.RightThumbY, AxisScale(smoothedY, true));
                            break;
                    }
                    return true;

                }
            }
            return false;
        }

        public bool setValue(string key, double value)
        {
            if (key.Length > 4 && key.ToLower().Substring(0, 4).Equals(PREFIX))
            {
                key = key.ToLower().Substring(4);
                //Make sure value is in range 0-1
                value = value > 1 ? 1 : value;
                value = value < 0 ? 0 : value;
                switch (key)
                {
                    case "sticklright":
                        device.report.SetAxis(Xbox360Axes.LeftThumbX, AxisScale(value, false));
                        break;
                    case "sticklleft":
                        device.report.SetAxis(Xbox360Axes.LeftThumbX, AxisScale(value, false));
                        break;
                    case "sticklup":
                        device.report.SetAxis(Xbox360Axes.LeftThumbY, AxisScale(value, true));
                        break;
                    case "stickldown":
                        device.report.SetAxis(Xbox360Axes.LeftThumbY, AxisScale(value, true));
                        break;
                    case "stickrright":
                        device.report.SetAxis(Xbox360Axes.RightThumbX, AxisScale(value, false));
                        break;
                    case "stickrleft":
                        device.report.SetAxis(Xbox360Axes.RightThumbX, AxisScale(value, false));
                        break;
                    case "stickrup":
                        device.report.SetAxis(Xbox360Axes.RightThumbY, AxisScale(value, true));
                        break;
                    case "stickrdown":
                        device.report.SetAxis(Xbox360Axes.RightThumbY, AxisScale(value, true));
                        break;
                    case "triggerr":
                        device.report.RightTrigger = (byte)(value * 255);
                        break;
                    case "triggerl":
                        device.report.LeftTrigger = (byte)(value * 255);
                        break;
                    default:
                        return false; //No valid key was found
                }
                return true;
            }
            return false;
        }

        public bool startUpdate()
        {
            return true;
        }

        public bool endUpdate()
        {
            if (viGEmClient.VigemTestClient != null)
            {
                device.Update();
                return true;
            }

            return false;
        }

        public bool reset()
        {
            device.Reset();
            return true;
        }

        private short AxisScale(double Value, bool Flip)
        {
            unchecked
            {
                float temp = (float)Value;
                if (Flip) temp = (temp - 0.5f) * -1.0f + 0.5f;

                return (short)(temp * ViGEmBus360Device.outputResolution + (-32768));
            }

            /*unchecked
            {
                Value -= 0x80;

                //float temp = (Value - (-128)) / (float)inputResolution;
                float temp = (Value - (-128)) * reciprocalInputResolution;
                if (Flip) temp = (temp - 0.5f) * -1.0f + 0.5f;

                return (short)(temp * outputResolution + (-32768));
            }
            */
        }
    }
}
