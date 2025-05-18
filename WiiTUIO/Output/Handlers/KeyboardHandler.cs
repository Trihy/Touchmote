using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output.Handlers
{
    public class KeyboardHandler : IButtonHandler
    {
        private HashSet<VirtualKeyCode> keysDown;

        public KeyboardHandler()
        {
            this.keysDown = new HashSet<VirtualKeyCode>();
        }

        public bool reset()
        {
            foreach(VirtualKeyCode keyCode in keysDown)
            {
                DS4Windows.InputMethods.performKeyRelease((ushort)keyCode);
            }

            keysDown.Clear();
            return true;
        }

        public bool setButtonDown(string key)
        {
            if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToUpper()))
            {
                VirtualKeyCode theKeyCode = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), key, true);
                DS4Windows.InputMethods.performKeyPress((ushort)theKeyCode);
                this.keysDown.Add(theKeyCode);
                return true;
            }
            return false;
        }

        public bool setButtonUp(string key)
        {
            if (Enum.IsDefined(typeof(VirtualKeyCode), key.ToUpper()))
            {
                VirtualKeyCode theKeyCode = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), key, true);
                DS4Windows.InputMethods.performKeyRelease((ushort)theKeyCode);
                this.keysDown.Remove(theKeyCode);
                return true;
            }
            return false;
        }

        public bool connect()
        {
            return true;
        }

        public bool disconnect()
        {
            return true;
        }

        public bool startUpdate()
        {
            return true;
        }

        public bool endUpdate()
        {
            return true;
        }
    }
}
