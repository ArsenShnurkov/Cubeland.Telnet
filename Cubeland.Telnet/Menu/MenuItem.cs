using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cubeland.Telnet {
    class MenuItem {
        public int ID;
        public bool SuppressDisplay = false;
        public String FormatString;
        public String MenuString;
        public String HelpString;
        public ItemValueTypes VType;
        public object Value;
        public int MaxValue;            // max string length, spinner val, slider val, &c.
    }
}
