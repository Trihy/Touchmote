﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output.Handlers
{
    interface ICursorHandler
    {

        bool updateState(long id, double x, double y);

    }
}