using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output.Handlers.Xinput
{
    public static class ViGEmBusClientSingleton
    {
        private static ViGEmBusClient defaultInstance;

        public static ViGEmBusClient Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = new ViGEmBusClient();
                }

                return defaultInstance;
            }
        }
    }
}
