using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cubeland.Telnet {
    class UIManager {

        public enum UIStates {
            NO_SUCH_STATE,
            LOGIN,
            USER_CREATE,
            USING_PROGRAM,
            LOGOUT
        }
    }
}
