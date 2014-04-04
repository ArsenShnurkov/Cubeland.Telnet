using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cubeland.Telnet {
    public static class TelnetDiff {

        // Simple string transform to minimize the number of characters sent over the wire.
        // Basically, step through each character in the new string and compare it to the
        // same location on the old string. If it is identical and !INIDENTITY, then 
        // set INIDENTITY. If identical and INIDENTITY, ignore. If not identical and
        // INIDENTITY, set !INIDENTITY and start a new string with MOVECURSORTO(x,y).
        // If not identical and !INIDENTITY, append to string.
        public static String Transform(String Base, String New, int ypos) {
            // Vt100Codes.MoveCursorTo();
            int xpos = 0;
            StringBuilder TempStr = new StringBuilder();
            TempStr.Append(Vt100Codes.MoveCursorTo(xpos, ypos));
            bool InIdentity = true;            
            for (int xx = 0; xx < New.Length; xx++) {
                if (xx < Base.Length) {
                    if (Base[xx] == New[xx] && !InIdentity) {
                        InIdentity = true;
                    } else if (Base[xx] != New[xx] && InIdentity) {
                        InIdentity = false;
                        TempStr.Append(Vt100Codes.MoveCursorTo(xpos,ypos));
                        TempStr.Append(New[xx]);
                    } else if (Base[xx] != New[xx] && !InIdentity) {
                        TempStr.Append(New[xx]);
                    } else {
                        // chars are identical and we're in the ignore stage
                    }
                } else { // we've overrun the length of the base string, so keep adding to SendStr
                    TempStr.Append(New[xx]);
                }
                xpos++;
            }
            while (xpos < Base.Length) {
                // add spaces to the string to remove any remaining chars
                TempStr.Append(' ');
            }
            return (TempStr.ToString());
        }
    }
}
