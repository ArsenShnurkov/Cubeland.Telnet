using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cubeland.Telnet.Menu {
    enum ItemActionTypes {
        NO_SUCH_ACTION,
        BUTTON,         // button item
        TEXT,           // scroll, no highlight
        HIGHLIGHT,      // scroll, highlight
        SELECT,         // select as active
        ENTRY,          // text entry field
        PASSWORD,       // password entry field
        SLIDER,         // slides characters
        SPINNER         // changes int32 value
    }
}
