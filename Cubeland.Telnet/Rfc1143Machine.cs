using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cubeland.Telnet {
    class Rfc1143Machine {

        NegState MyState;
        bool MyQueue;
        NegState TheirState;
        bool TheirQueue;

        public enum NegState {
            NO_SUCH_STATE,
            NO,
            WANTNO,
            WANTYES,
            YES
        }
    }
}
