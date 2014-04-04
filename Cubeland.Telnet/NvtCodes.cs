using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cubeland.Telnet {

    /*
     *  VT100 SPECIAL CHARACTERS
        q	─
        w	┬
        e	[LF]
        r 	_
        t	├
        y	≤
        u	┤
        i	♂
        o	⌐
        p	⌐
        [	[
        ]	]
        \	♦
        a	▒
        s	_
        d	[CR] 
        f	°
        g	±
        h	h
        j	┘
        k	┐
        l	┌
        ;	;
        '	'
        z	≥
        x	│
        c	♀
        v	┴
        b	[HT]
        n	┼
        m	└
        ,	,
        .	.
        /	/
        `	♦
        ~	■
        _	█
        {	π
        }	£
        |	/

        lqqqqqqqqqqqqqqqqqqwqqqqqqqqqqqqqqqqqqqqk
        x                  x                    x
        x                  x                    x
        tqqqqqqqqqqqqqqqqqqnqqqqqqqqqqqqqqqqqqqqu
        x                  x                    x
        x                  x                    x
        mqqqqqqqqqqqqqqqqqqvqqqqqqqqqqqqqqqqqqqqj

     * Example of setting the terminal in G1 special graphics mode, then using SO to shift to G1
      String s = (Vt100Codes.SetG1Special);
      byte[] ba = ASCIIEncoding.UTF8.GetBytes(s);
      SendBuffer[0] = Vt100Codes.GetByte(NvtCodes.SO);
      Array.Copy(ba, 0, SendBuffer, 1, ba.Length);
      Sender.SetBuffer(0, (ba.Length + 1));
     */


    static class Vt100Codes {

        public static String EscapeCode = ((char)27).ToString();

        public static String SetUSG0 = EscapeCode + "(B";
        public static String SetUSG1 = EscapeCode + ")B";
        public static String SetG0Special = EscapeCode + "(0";
        public static String SetG1Special = EscapeCode + ")0";

        public static String SetVt52GraphicsMode = EscapeCode + "[F";
        public static String SetVt52TextMode = EscapeCode + "[G";


        public static String LineWrapOn = EscapeCode + "[7h";
        public static String LineWrapOff = EscapeCode + "[7l";

        public static String AllCharModesOff = EscapeCode + "[m";
        public static String CharModeOff = EscapeCode + "[0m";
        public static String CharBright = EscapeCode + "[1m";
        public static String CharDim = EscapeCode + "[2m";
        public static String CharUL = EscapeCode + "[4m";
        public static String CharBlink = EscapeCode + "[5m";
        public static String CharReverse = EscapeCode + "[7m";

        public static String CharFgBlack = EscapeCode + "[30m";
        public static String CharFgRed = EscapeCode + "[31m";
        public static String CharFgGreen = EscapeCode + "[32m";
        public static String CharFgYellow = EscapeCode + "[33m";
        public static String CharFgBlue = EscapeCode + "[34m";
        public static String CharFgMagenta = EscapeCode + "[35m";
        public static String CharFgCyan = EscapeCode + "[36m";
        public static String CharFgWhite = EscapeCode + "[37m";

        public static String CharBgBlack = EscapeCode + "[40m";
        public static String CharBgRed = EscapeCode + "[41m";
        public static String CharBgGreen = EscapeCode + "[42m";
        public static String CharBgYellow = EscapeCode + "[43m";
        public static String CharBgBlue = EscapeCode + "[44m";
        public static String CharBgMagenta = EscapeCode + "[45m";
        public static String CharBgCyan = EscapeCode + "[46m";
        public static String CharBgWhite = EscapeCode + "[47m";

        public static String ClearScreen = EscapeCode + "[2J";
        public static String ClearLine = EscapeCode + "[2K";

        public static String ClientArrowUp = EscapeCode + "[A";
        public static String ClientArrowDown = EscapeCode + "[B";
        public static String ClientArrowRight = EscapeCode + "[C";
        public static String ClientArrowLeft = EscapeCode + "[D";

        public static String SaveCursorPos = EscapeCode + "[s";
        public static String RestoreCursorPos = EscapeCode + "[u";

        public static String MoveCursorByLines(int up, int down, int left, int right) {
            String s = (EscapeCode + "[" + up + "A" + EscapeCode + "[" + down + "B" + EscapeCode + "[" + left + "D" + EscapeCode + "[" + right + "C");
            return (s);
        }
        public static String MoveCursorHome() {
            return (EscapeCode + "[[H");
        }
        public static String MoveCursorTo(int x, int y) {
            return (EscapeCode + "[" + y + ";" + x + "H");
        }
        public static string MoveToNextLine() {
            return (EscapeCode + "[E");
        }

    }

    static class NvtCodes {
        // STANDARD ASCII CONTROL CODES
        public static byte NULL = 0;           // vt100: ignored
        public static byte SOH = 1;
        public static byte STX = 2;
        public static byte ETX = 3;
        public static byte EOT = 4;
        public static byte ENQ = 5;            // vt100: send ANSWERBACK
        public static byte ACK = 6;
        public static byte BEL = 7;            // vt100: ring bell
        public static byte BS = 8;             // vt100: move cursor left one pos unless at left margin
        public static byte TAB = 9;            // vt100: move to next tabstop or right margin
        public static byte LF = 10;            // vt100: linefeed or newline (if in linemode)
        public static byte VT = 11;            // vt100: same as LF
        public static byte FF = 12;            // vt100: same as LF
        public static byte CR = 13;            // vt100: move cursor to left margin
        public static byte SO = 14;            // vt100: invoke G1 charset
        public static byte SI = 15;            // vt100: invoke G0 charset
        public static byte DLE = 16;
        public static byte DC1 = 17;           // vt100: XON/resume transmission
        public static byte DC2 = 18;
        public static byte DC3 = 19;           // vt100: XOFF/stop transmission
        public static byte DC4 = 20;
        public static byte NAK = 21;
        public static byte SYN = 22;
        public static byte ETB = 23;
        public static byte CAN = 24;           // vt100: terminal current control sequence and display checkerboard
        public static byte EM = 25;
        public static byte SUB = 26;           // vt100: same as CAN
        public static byte ESC = 27;           // vt100: introduce a control sequence
        public static byte FS = 28;
        public static byte GS = 29;
        public static byte RS = 30;
        public static byte US = 31;


        // TELNET COMMANDS
        public static byte SE = 240;       // end subnegotiation parameters
        public static byte NOP = 241;       // no operation
        public static byte DM = 242;       // data mark (synch event, add TCP urgent)
        public static byte BRK = 243;      // break/attention
        public static byte IP = 244;       // interrupt/suspend/abort
        public static byte AO = 245;       // abort output
        public static byte AYT = 246;      // are you there?
        public static byte EC = 247;       // erase character
        public static byte EL = 248;       // erase line
        public static byte GA = 249;       // go ahead
        public static byte SB = 250;       // begin subnegotiation
        public static byte WILL = 251;     // will begin or are beginning operation
        public static byte WONT = 252;     // will not perform operation
        public static byte DO = 253;       // request operation
        public static byte DONT = 254;     // request stop or nonperformance of operation
        public static byte IAC = 255;      // interpret as command

        // AUTHENTICATION TYPES FOR OPT 37/AUTH_OPT/RFC 2941
        public static byte IS = 0;
        public static byte SEND = 1;
        public static byte REPLY = 2;
        public static byte NAME = 3;
        public static byte AUTH_NULL = 0;
        public static byte KERBEROS_V4 = 1;
        public static byte KERBEROS_V5 = 2;
        public static byte SPX = 3;
        public static byte MINK = 4;
        public static byte SRP = 5;
        public static byte RSA = 6;
        public static byte LOKI = 10;
        public static byte SSA = 11;
        public static byte KEA_SJ = 12;
        public static byte KEA_SJ_INTEG = 13;
        public static byte DSS = 14;
        public static byte NTLM = 15;

        // RFC 1411 KERBEROS V4 & V5 SUBOPTS
        public static byte K4_AUTH = 0;
        public static byte K4_REJECT = 1;
        public static byte K4_ACCEPT = 2;
        public static byte K4_CHALLENGE = 3;
        public static byte K4_RESPONSE = 4;
        public static byte K4_FWD = 5;
        public static byte K4_FWD_ACCEPT = 6;
        public static byte K4_FWD_REJECT = 7;
        public static byte K4_EXP = 8;
        public static byte K4_PARAMS = 9;
        public static byte K5_AUTH = 0;
        public static byte K5_REJECT = 1;
        public static byte K5_ACCEPT = 2;
        public static byte K5_RESPONSE = 3;
        public static byte K5_FWD = 4;
        public static byte K5_FWD_ACCEPT = 5;
        public static byte K5_FWD_REJECT = 6;

        // RFC 2943 SUBOPTS
        public static byte DSS_INITIALIZE = 1;
        public static byte DSS_TOKENBA = 2;
        public static byte DSS_CERTA_TOKENAB = 3;
        public static byte DSS_CERTB_TOKENBA2 = 4;

        // RFC 2944
        public static byte SRP_AUTH = 0;
        public static byte SRP_REJECT = 1;
        public static byte SRP_ACCEPT = 2;
        public static byte SRP_CHALLENGE = 3;
        public static byte SRP_RESPONSE = 4;
        public static byte SRP_EXP = 8;
        public static byte SRP_PARAMS = 9;

        public static byte KEA_CERTA_RA = 1;
        public static byte KEA_CERTB_RB_IVB_NONCEB = 2;
        public static byte KEA_IVA_RESPONSEB_NONCEA = 3;
        public static byte KEA_RESPONSEA = 4;


        // OPTIONS

        public static byte TRANSMIT_BINARY = 0;
        public static byte ECHO = 1;                       // IAC WILL ECHO means the server will send back every character received
        public static byte RECONNECTION = 2;
        public static byte SUPPRESS_GO_AHEAD = 3;          // IAC DONT SUPPRESS_GO_AHEAD to stop half-duplex signaling with GA
        public static byte APPROX_MSG_SZ = 4;
        public static byte STATUS = 5;
        public static byte TIMING_MARK = 6;                 // if a server sends a IAC DO TIMING_MARK request to a compliant client,
                                                            // the client will insert the timing mark into the outgoing bytestream
                                                            // so that it appears at the server's incoming bytestream at the point
                                                            // in client input that the client received the request.
        public static byte REMOTE_CTL_TRANS_ECHO = 7;       // lets server control echo behavior
        public static byte OUTPUT_LINE_WIDTH = 8;
        public static byte OUTPUT_PAGE_SIZE = 9;
        public static byte OUTPUT_CR_DISP = 10;
        public static byte OUTPUT_HZ_TAB = 11;
        public static byte OUTPUT_HZ_TAB_DISP = 12;
        public static byte OUTPUT_FF_DISP = 13;
        public static byte OUTPUT_V_TAB = 14;
        public static byte OUTPUT_V_TAB_DISP = 15;
        public static byte OUTPUT_LF_DISP = 16;
        public static byte EXTENDED_ASCII = 17;
        public static byte LOGOUT = 18;
        public static byte BYTE_MACRO = 19;
        public static byte DATA_ENTRY_TERMINAL = 20;
        public static byte SUPDUP = 21;
        public static byte SUPDUP_OUTPUT = 22;
        public static byte SEND_LOCATION = 23;
        public static byte TERMINAL_TYPE = 24;
        public static byte END_OF_REC = 25;
        public static byte TACACS_USER_ID = 26;
        public static byte OUTMRK = 27;                     // persistent banner marking (IAC WILL OUTMRK, IAC SB OUTMRK CNTL data IAC SC)
        public static byte TERM_LOC_NUM = 28;
        public static byte TELNET_3270_REGIMES = 29;
        public static byte X3_PAD = 30;
        public static byte NAWS = 31;                       // negotiate window size (cl: IAC WILL NAWS sv: IAC DO NAWS cl: IAC SB NAWS 16bv 16bv IAC SE)
        public static byte TERMINAL_SPEED = 32;
        public static byte REMOTE_FLOW_CONTROL = 33;
        public static byte LINEMODE = 34;                  // IAC WILL LINEMODE & IAC DO LINEMODE means assemble lines at client end and send over
        public static byte X_DISPLAY_LOC = 35;
        public static byte ENV_VARS = 36;
        public static byte AUTH_OPT = 37;
        public static byte ENCRYPT_OPT = 38;
        public static byte NEW_ENV_OPT = 39;
        public static byte TN3270E = 40;
        public static byte XAUTH = 41;
        public static byte CHARSET = 42;
        public static byte TELNET_RSP = 43;
        public static byte COM_PORT_CTL = 44;
        public static byte TELNET_SUPPRESS_LOCAL_ECHO = 45;
        public static byte TELNET_START_TLS = 46;
        public static byte KERMIT = 47;
        public static byte SEND_URL = 48;
        public static byte FORWARD_X = 49;
        public static byte TELOPT_PRAGMA_LOGON = 138;
        public static byte TELOPT_SSPI_LOGON = 139;
        public static byte TELOPT_PRAGMA_HEARTBEAT = 140;
        public static byte EXT_OPT_LIST = 255;                     // extended options list

        public static String GetNvtString(byte b) {
            switch (b) {
                case 1:
                    return ("ECHO");
                case 2:
                    return ("RECONNECTION");
                case 3:
                    return ("SUPPRESS_GOAHEAD");
                case 31:
                    return ("NAWS");
                case 34:
                    return ("LINEMODE");
                case 35:
                    return ("X_DISPLAY_LOC");
                case 240:
                    return ("SE");
                case 241:
                    return ("NOP");
                case 250:
                    return ("SB");
                case 251:
                    return ("WILL");
                case 252:
                    return ("WONT");
                case 253:
                    return ("DO");
                case 254:
                    return ("DONT");
                case 255:
                    return ("IAC");
                default:
                    return (Convert.ToString(b));
            }
        }

    }
}
