using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output
{
    internal class FakerInputDevice : FakerInputWrapper.FakerInput
    {
        private static FakerInputDevice currentInstance;

        public static FakerInputDevice Current
        {
            get
            {
                if (currentInstance == null)
                {
                    currentInstance = new FakerInputDevice();
                    if (!currentInstance.Connect())
                    {
                        Console.WriteLine("Could not connect to the FakerInput device");
                    }
                    else
                    {
                        Console.WriteLine("FAKERINPUT CONNECTION ESTABLISHED");
                    }
                }

                return currentInstance;
            }
        }

        public bool isAvailable()
        {
            return this.IsConnected();
        }
    }
}
