﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Transfer
{
    [Serializable]
    public class ModelStatus
    {
        public int Status { get; set; }
        public string Message { get; set; }
    }
}
