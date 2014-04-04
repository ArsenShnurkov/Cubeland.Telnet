using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cubeland.Telnet {
    public enum PayloadType {
        NO_SUCH_TYPE,
        KEY,
        BYTE_ARRAY        
    }

    public struct NetPayload {       
        public Int64 NetClientID;
        public PayloadType Type;
        public KeybdKeys Key;
        public byte[] ByteString;
    }
}
