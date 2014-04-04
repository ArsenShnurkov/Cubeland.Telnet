#define test

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace Cubeland.Telnet {
    public class NetConnection {

        public Int64 UserID;
        public Int64 ActorID;

        private Int64 NetClientID;
        public Socket AcceptSocket;
        SocketAsyncEventArgs Sender;
        SocketAsyncEventArgs Receiver;
        public byte[] ReceptorBuffer = new byte[2048];
        public byte[] SendBuffer = new byte[2048];
        public int ScreenX;
        public int ScreenY;
        
        public List<byte> ConsumerBuf;
        public List<byte> ProviderBuf;
        Action<Int64> EndSession;
        Action<Object> RoutePayloads;
        bool EchoBackInLinemode = true;
        int LastCharEchoed = 0;

        CharacterModes Mode;
        public MenuGrid Grid;
        public Stack<MenuGrid> GridStack;


        public enum CharacterModes {
            NO_SUCH_MODE,
            IMMEDIATE,
            LINEMODE
        }

        private enum CharSteps {
            NO_SUCH_CHAR,
            CR,
            LF,
            CRLF,
            ESC,

            ESC_91,
            ESC_91_65,          // up arrow
            ESC_91_66,          // down arrow
            ESC_91_67,          // right arrow
            ESC_91_68,          // left arrow

            ESC_79,
            ESC_79_80,          // F1
            ESC_79_81,          // F2
            ESC_79_82,          // F3
            ESC_79_83,          // F4

            ESC_91_53_126,   // PGUP
            ESC_91_54,
            ESC_91_54_126,   // PGDN

            ESC_91_49,
            ESC_91_49_53,
            ESC_91_49_53_126,   // F5		ESC_[_1_5_~
            ESC_91_49_55,
            ESC_91_49_55_126,   // F6		ESC_[_1_7_~
            ESC_91_49_56,
            ESC_91_49_56_126,   // F7		ESC_[_1_8_~
            ESC_91_49_57,
            ESC_91_49_57_126,   // F8		ESC_[_1_9_~
            ESC_91_49_48,
            ESC_91_49_48_126,   // F9		ESC_[_1_0_~
            ESC_91_49_49,
            ESC_91_49_49_126,   //F10		ESC_[_1_1_~
            ESC_91_49_51,
            ESC_91_49_51_126,   // F11		ESC_[_1_3_~
            ESC_91_49_52,	
            ESC_91_49_52_126    // F12		ESC_[_1_2_~

        }

        private class BufferPayload {
            public KeybdKeys Key;
            public byte[] Data;
        }

        public NetConnection(Int64 id, Socket socket, SocketAsyncEventArgs sender, SocketAsyncEventArgs receiver, Action<Int64> es, Action<Object> np) {
            ConsumerBuf = new List<byte>();
            ProviderBuf = new List<byte>();
            AcceptSocket = socket;
            NetClientID = id;
            Sender = sender;
            Receiver = receiver;
            EndSession = es;
            RoutePayloads = np;
            Mode = CharacterModes.LINEMODE;
        }

        public void ClearBuffer() {
            lock (ConsumerBuf) {
                ConsumerBuf.Clear();
            }
        }

        // we step through the Consumer Buffer, looking for the following things:
        //
        // - If we see an IAC, attempt to match a negotiation attempt and handle accordingly
        //
        // - If in immediate mode, send all special keys and printable ASCIIs to the server logic
        //
        // - If in linemode, look for a CRLF and send over any printable ASCIIs buffered until then
        //
        // - If in linemode_with_immediate, collect every special key and send them over in order, then look
        //   for a CRLF and send over any printable ASCIIs buffered until then
        //
        private void ScanBuffer(object sz) {
#if supertest
            Logger.Log("Start scan with " + ConsumerBuf.Count() + " to go.");
#endif
            lock (ConsumerBuf) {
                // REMCODE 01A
                bool IsESCCode = false;
                bool IsTelnetNeg = false;
                bool IsCRLF = true;
                Stack<int> RemList = new Stack<int>(); // we use a stack so we don't disturb the ordering of the bytes we're removing
                CharSteps LastStep = CharSteps.NO_SUCH_CHAR;
                List<NetPayload> Payload = new List<NetPayload>();
                int xx = 0;
                int Len = ConsumerBuf.Count();
                while (xx < Len) {
                    byte cb = ConsumerBuf[xx];
                    if (cb == NvtCodes.IAC && !IsTelnetNeg) {
                        IsTelnetNeg = true;
                        // let's advance through and see if we can get a telnet neg or subneg

                        // IAC NOP is used as a keepalive; right now, we ignore it
                        if (xx < (Len - 1) && ConsumerBuf[xx+1] == NvtCodes.NOP) {
                            RemList.Push(xx);
                            xx++;
                            RemList.Push(xx);
                            IsTelnetNeg = false;
                          // next, check to see if it's an IAC [DO|DONT|WILL|WONT] tuple
                        } else if (xx < (Len - 2) && ConsumerBuf[xx + 1] >= 251 && ConsumerBuf[xx + 1] <= 254) {
                            // TODO: handle negotiations
                            #if test
                                Logger.Log(NvtCodes.GetNvtString(ConsumerBuf[xx]) + "|" + NvtCodes.GetNvtString(ConsumerBuf[xx+1]) + "|" + NvtCodes.GetNvtString(ConsumerBuf[xx+2]));                            
                            #endif
                            RemList.Push(xx);
                            xx++;
                            RemList.Push(xx);
                            xx++;
                            RemList.Push(xx);
                            IsTelnetNeg = false;
                          // check for IAC SB ... IAC SE for subnegotiation parameters
                        } else if (xx < (Len - 1) && ConsumerBuf[xx + 1] == NvtCodes.SB) {
                            // handle NAWS subneg
                            if (ConsumerBuf[xx] == NvtCodes.IAC && ConsumerBuf[xx + 1] == NvtCodes.SB && ConsumerBuf[xx + 2] == NvtCodes.NAWS) {
                                // TODO: This doesn't handle 255s properly (255s should be doubled in the bytestream to prevent confusion with IAC)
                                if (BitConverter.IsLittleEndian) {
                                    ScreenX = Convert.ToInt32(BitConverter.ToInt16(new byte[] { ConsumerBuf[xx + 4], ConsumerBuf[xx + 3] }, 0));
                                    ScreenY = Convert.ToInt32(BitConverter.ToInt16(new byte[] { ConsumerBuf[xx + 6], ConsumerBuf[xx + 5] }, 0));
                                } else {
                                    ScreenX = Convert.ToInt32(BitConverter.ToInt16(new byte[] { ConsumerBuf[xx + 3], ConsumerBuf[xx + 4] }, 0));
                                    ScreenY = Convert.ToInt32(BitConverter.ToInt16(new byte[] { ConsumerBuf[xx + 5], ConsumerBuf[xx + 6] }, 0));
                                }
                                #if test
                                    Logger.Log("ID " + NetClientID + " now has scrsz " + ScreenX + "x" + ScreenY);
                                #endif
                            }
                            int yy = xx + 1;
                            bool FoundSE = false;
                            while (yy < Len && !FoundSE) {
                                if (ConsumerBuf[yy] == NvtCodes.IAC && yy < (Len-1)) {
                                    if (ConsumerBuf[yy + 1] == NvtCodes.SE) {
                                        FoundSE = true;
                                        IsTelnetNeg = false;
                                    }
                                }
                                yy++;
                            }
                            if (FoundSE) {
                                String s = "";
                                // we found a subneg! Right now, log it.
                                for (int zz = xx; zz <= yy; zz++) {
                                    s = s + NvtCodes.GetNvtString(ConsumerBuf[zz]) + "|";
                                    #if supertest
                                        Logger.Log("Pushing " + zz + " with bufsz " + ConsumerBuf.Count());
                                    #endif
                                    RemList.Push(zz);
                                }
                                #if test
                                    Logger.Log(s);
                                #endif
                            } else {
                                Logger.Log("Failed to find IAC SE in telnet subneg between " + xx + " and " + yy);
                            }
                        }
                       // look for ESC codes
                        IsTelnetNeg = false;
                    } else if (cb == 27 && IsTelnetNeg == false && IsESCCode == false) {
                        IsESCCode = true;
                        if (xx < (Len - 2) && ConsumerBuf[xx + 1] == 79) {
                            // we might be looking at an F1 ... F4 key
                            bool IsFKey = false;
                            switch (ConsumerBuf[xx + 2]) {
                                case 80:
                                    IsFKey = true;
                                    #if supertest
                                    Logger.Log("F1");
                                    #endif
                                    Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.F1 });
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    IsESCCode = false;
                                    break;
                                case 81:
                                    IsFKey = true;
                                    #if supertest
                                    Logger.Log("F2");
                                    #endif
                                    Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.F2 });
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    IsESCCode = false;
                                    break;
                                case 82:
                                    IsFKey = true;
                                    #if supertest
                                    Logger.Log("F3");
                                    #endif
                                    Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.F3 });
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    IsESCCode = false;
                                    break;
                                case 83:
                                    IsFKey = true;
                                    #if supertest
                                    Logger.Log("F4");
                                    #endif
                                    Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.F4 });
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    IsESCCode = false;
                                    break;
                                default:
                                    break;
                            }
                        } else if (xx < (Len - 2) && ConsumerBuf[xx + 1] == 91) {
                            // could be any number of other characters
                            switch (ConsumerBuf[xx + 2]) {
                                // 65 ... 68 are arrow keys; 53.126 and 54.126 are PGUP and PGDN;
                                // 49 plus four more chars are other F keys
                                case 65:
                                    // UP
                                    #if supertest
                                    Logger.Log("UP");
                                    #endif
                                    Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.UP });
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    IsESCCode = false;
                                    break;
                                case 66:
                                    // DOWN
                                    #if supertest
                                    Logger.Log("DOWN");
                                    #endif
                                    Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.DOWN });
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    IsESCCode = false;
                                    break;
                                case 67:
                                    // RIGHT
                                    #if supertest
                                    Logger.Log("RIGHT");
                                    #endif
                                    Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.RIGHT });
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    IsESCCode = false;
                                    break;
                                case 68:
                                    // LEFT
                                    #if supertest
                                    Logger.Log("LEFT");
                                    #endif
                                    Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.LEFT });
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    xx++;
                                    RemList.Push(xx);
                                    IsESCCode = false;
                                    break;
                                case 51:
                                    // Maybe DEL?
                                    if (xx < (Len - 3)) {
                                        if (ConsumerBuf[xx + 3] == 126) {
                                            #if supertest
                                            Logger.Log("DELETE");
                                            #endif
                                            Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.DELETE });
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            IsESCCode = false;
                                        }
                                    }
                                    break;
                                case 52:
                                    // Maybe SEL?
                                    if (xx < (Len - 3)) {
                                        if (ConsumerBuf[xx + 3] == 126) {
                                            #if supertest
                                            Logger.Log("SELECT");
                                            #endif
                                            Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.SELECT });
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            IsESCCode = false;
                                        }
                                    }
                                    break;
                                case 53:
                                    // Maybe PGUP?
                                    if (xx < (Len - 3)) {
                                        if (ConsumerBuf[xx + 3] == 126) {
                                            #if supertest
                                            Logger.Log("PGUP");
                                            #endif
                                            Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.PGUP });
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            IsESCCode = false;
                                        }
                                    }
                                    break;
                                case 54:
                                    // Maybe PGDN?
                                    if (xx < (Len - 3)) {
                                        if (ConsumerBuf[xx + 3] == 126) {
                                            #if supertest
                                            Logger.Log("PGDN");
                                            #endif
                                            Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.PGDN });
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            IsESCCode = false;
                                        }
                                    }
                                    break;
                                case 49:
                                    // Maybe FIND?
                                    if (xx < (Len - 3)) {
                                        if (ConsumerBuf[xx + 3] == 126) {
                                            #if supertest
                                            Logger.Log("FIND");
                                            #endif
                                            Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.FIND });
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            IsESCCode = false;
                                            break;
                                        }
                                    }

                                    // Maybe F5 ... F8
                                    if (xx < (Len - 4) && ConsumerBuf[xx + 4] == 126) {
                                        switch (ConsumerBuf[xx + 3]) {
                                            case 53:
                                                // F5
                                                #if supertest
                                                Logger.Log("F5");
                                                #endif
                                                Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.F5 });
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                IsESCCode = false;
                                                break;
                                            case 55:
                                                // F6
                                                #if supertest
                                                Logger.Log("F6");
                                                #endif
                                                Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.F6 });
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                break;
                                            case 56:
                                                // F7
                                                #if supertest
                                                Logger.Log("F7");
                                                #endif
                                                Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.F7 });
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                break;
                                            case 57:
                                                // F8
                                                #if supertest
                                                Logger.Log("F8");
                                                #endif
                                                Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.F8 });
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    break;
                                case 50:
                                    // Maybe INSERT?
                                    if (xx < (Len - 3)) {
                                        if (ConsumerBuf[xx + 3] == 126) {
                                            #if supertest
                                            Logger.Log("INSERT");
                                            #endif
                                            Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.INSERT });
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            xx++;
                                            RemList.Push(xx);
                                            IsESCCode = false;
                                            break;
                                        }
                                    }

                                    // F9 ... F12?
                                    if (xx < (Len - 4) && ConsumerBuf[xx + 4] == 126) {
                                        switch (ConsumerBuf[xx + 3]) {
                                            case 48:
                                                // F9
                                                #if supertest
                                                Logger.Log("F9");
                                                #endif
                                                Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.F9 });
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                break;
                                            case 49:
                                                // F10
                                                #if supertest
                                                Logger.Log("F10");
                                                #endif
                                                Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.F10 });
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                break;
                                            case 51:
                                                // F11
                                                #if supertest
                                                Logger.Log("F11");
                                                #endif
                                                Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.F11 });
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                break;
                                            case 52:
                                                // F12
                                                #if supertest
                                                Logger.Log("F12");
                                                #endif
                                                Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.KEY, Key = KeybdKeys.F12 });
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                xx++;
                                                RemList.Push(xx);
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        } else {
                            RemList.Push(xx);
                        }
                        IsESCCode = false;
                    } else if (Mode == CharacterModes.IMMEDIATE) {
                        // are we doing anything here?
                        if (ConsumerBuf[xx] == 8 || (ConsumerBuf[xx] >= 32 && ConsumerBuf[xx] <= 127)) {
                            Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.BYTE_ARRAY, ByteString=new byte[1]{ConsumerBuf[xx]} });
                            #if supertest
                            Logger.Log("Got immediate mode char " + Convert.ToChar(ConsumerBuf[xx]));
                            #endif
                            RemList.Push(xx);
                        } else {
                            Logger.Log("Orphan byte:" + ConsumerBuf[xx]);
                            RemList.Push(xx);
                        }
                    }
                    xx++;
                }
                // remove inspected characters
                lock (ConsumerBuf) {
                    foreach (int i in RemList) {
                        #if supertest
                        Logger.Log("Removing " + i + " of " + ConsumerBuf.Count());
                        #endif
                        if (i < ConsumerBuf.Count()) {
                            ConsumerBuf.RemoveAt(i);
                        } else {
                            Logger.Log(i + " not in range " + ConsumerBuf.Count());
                        }
                    }
                }
                RemList.Clear();

                // scan for CRLF
                if (Mode == CharacterModes.LINEMODE) {
                    List<String> cmd = new List<string>();          // command strings
                    List<byte> Accum = new List<byte>();            // accumulation buffer for building cmds
                    List<int> RunningClear = new List<int>();       // list of chars to remove
                    bool FoundCRLF = false;
                    for (int xy = 0; xy < ConsumerBuf.Count(); xy++) {
                        if (ConsumerBuf[xy] == 13 && xy < (ConsumerBuf.Count() - 1) && ConsumerBuf[xy + 1] == 10) {
                            FoundCRLF = true;
                            xy++;
                            String s = "";
                            foreach (byte b in Accum) {
                                s = s + Convert.ToChar(b);
                            }
                            cmd.Add(s);
                            Accum.Clear();
                            foreach (int i in RunningClear) {
                                RemList.Push(i);
                            }
                            RemList.Push(xy - 1);
                            RemList.Push(xy);
                        } else if (ConsumerBuf[xy] == 8 || (ConsumerBuf[xy] >= 32 && ConsumerBuf[xy] <= 127)) {
                            if (ConsumerBuf[xy] == 8) {
                                // backspace
                                if (Accum.Count() > 0) {
                                    Accum.RemoveAt((Accum.Count() - 1));
                                }
                                RunningClear.Add(xy);
                            } else if (ConsumerBuf[xy] == 127) {
                                // delete
                                if (Accum.Count() > 0) {
                                    Accum.RemoveAt((Accum.Count() - 1));
                                }
                                RunningClear.Add(xy);
                            } else {
                                Accum.Add(ConsumerBuf[xy]);
                                RunningClear.Add(xy);
                            }
                        } else {
                            Logger.Log("Unknown byte: " + ConsumerBuf[xy]);
                        }
                    }

                    foreach (int i in RemList) {
                        ConsumerBuf.RemoveAt(i);
                    }
                    RemList.Clear();

                    if (cmd.Count() > 0) {
                        foreach (String s in cmd) {
                            Payload.Add(new NetPayload() { NetClientID = NetClientID, Type = PayloadType.BYTE_ARRAY, ByteString = ASCIIEncoding.UTF8.GetBytes(s) });
                            #if supertest
                                Logger.Log("Got linemode cmd: " + s);
                            #endif

                        }
                        // TODO: This is a temporary hack to send back a CRLF
                        SendMsg(0, new byte[2] { 13, 10 });
                    } else {
                        // TODO: Another temporary hack to send back chars on the single line
                        // These should nail the prompt to the bottom line of the telnet client
                        if (Accum.Count() > 0) {
                            byte[] ba = new byte[6];
                            // ESC[2K
                            ba[0] = 27;
                            ba[1] = 91;
                            ba[2] = 50;
                            ba[3] = 75;
                            ba[4] = 13;
                            ba[5] = 62;

                            int rstart = 0;
                            int rend = Accum.Count();   // length of array

                            // TODO: note that this assumes a single-char prompt
                            // ex: count 20, x 20
                            if (rend >= ScreenX) {      // = because of prompt
                                rstart = (rend + 1) - ScreenX;
                                rend = rend - rstart;
                            }

                            // BUG note that sending data back from a telnet session too (prior to negotiation completion?)
                            // throws an unhandled exception here
                            SendMsg(0, ba);
                            SendMsg(0, Accum.GetRange(rstart, rend).ToArray<byte>());
                        }
                    }
                    
                }
                Thread Router = new Thread(new ParameterizedThreadStart(RoutePayloads));
                Router.Start(Payload.ToArray<NetPayload>());

                #if test
                    foreach(NetPayload np  in Payload){
                        String s = "";
                        if (np.Type == PayloadType.KEY) {
                            s = np.NetClientID + ": " + np.Key;
                        } else if (np.Type == PayloadType.BYTE_ARRAY) {
                            s = np.NetClientID + ": ";
                            foreach (byte b in np.ByteString) {
                                s = s + Convert.ToChar(b);
                            }
                        }
                        Logger.Log(s);
                    }
                #endif

            }
            #if supertest
                Logger.Log("End scan with " + ConsumerBuf.Count() + " to go.");
            #endif
        }

        public void SendMsg(int Location, String s) {
            SendMsg(Location, ASCIIEncoding.UTF8.GetBytes(s));                
        }

        public void SendMsg(int Location, byte[] ba) {
            lock (ProviderBuf) {
                foreach (byte b in ba) {
                    ProviderBuf.Add(b);
                }
            }            
        }


        // TODO: Right now we're just messing around with the negotiations
        public void Negotiate() {
            Logger.Log("Negotiating...");
            List<byte> lb = new List<byte>();
            lb.Add(NvtCodes.IAC);
            lb.Add(NvtCodes.DO);
            lb.Add(NvtCodes.NAWS);
            lb.Add(NvtCodes.IAC);
            lb.Add(NvtCodes.WILL);
            lb.Add(NvtCodes.ECHO);
            SendMsg(0, lb.ToArray());    
            
        }

        public Int64 GetID() {
            return (NetClientID);
        }

        public void Send(object sender, SocketAsyncEventArgs args) {
            DoSend(args, true);
        }

        private void DoSend(SocketAsyncEventArgs args, bool ResetRead) {

            if (args.SocketError != SocketError.Success) {
                if (args.SocketError == SocketError.ConnectionReset) {
                    EndSession(NetClientID);
                }

            }


            lock (SendBuffer) {
                Array.Clear(SendBuffer, 0, SendBuffer.Length);
                lock (ProviderBuf) {
                    int ct = ProviderBuf.Count();
                    if (ct > 0) {
                        for (int wx = 0; wx < ct; wx++) {
                            SendBuffer[wx] = ProviderBuf[wx];
                        }
                        ProviderBuf.Clear();
                    }
                    Sender.SetBuffer(SendBuffer, 0, ct);
                }                
            }            

            if (AcceptSocket.Connected && ResetRead) {
                bool WillRaiseEvent = AcceptSocket.SendAsync(Sender);
                if (!WillRaiseEvent) {
                    DoSend(args, true);
                }
            }                       
        }

        public void Receive(Object sender, SocketAsyncEventArgs args) {
            DoReceive(args);
        }

        // BUG probably doesn't work.
        public void ChangeMode(CharacterModes m){
            if(m == CharacterModes.IMMEDIATE){
                Mode = CharacterModes.IMMEDIATE;
                // ...
            } else if (m == CharacterModes.LINEMODE){
                Mode = CharacterModes.LINEMODE;
                // ...
            }
        }

        private void DoReceive(SocketAsyncEventArgs args) {
            if (args.SocketError != SocketError.Success) {
                if (args.SocketError == SocketError.ConnectionReset) {
                    EndSession(NetClientID);
                }
            }
            lock (ReceptorBuffer) {
                #if supertest
                    Logger.Log("Got bytecount " + args.BytesTransferred);


                    String s = "";
                    for (int xx = 0; xx < args.BytesTransferred; xx++) {
                        s = s + ReceptorBuffer[xx] + ":";
                    }
                    Logger.Log("Val:" + s);
                #endif
                // dump the data into a queue for separate processing; while testing, we'll do it on the thread
                lock (ConsumerBuf) {
                    byte[] b = new byte[args.BytesTransferred];
                    Array.ConstrainedCopy(ReceptorBuffer, 0, b, 0, args.BytesTransferred);
                    ConsumerBuf.AddRange(b);
                }
            }
            // do thread stuff here
            Thread scanThread = new Thread(ScanBuffer);
            scanThread.Start(args.BytesTransferred);

                    
            if(AcceptSocket.Connected) {
                bool WillRaiseEvent = AcceptSocket.ReceiveAsync(Receiver);
                if (!WillRaiseEvent) {
                    DoReceive(Receiver);
                }
            }                     
                
        }

        private void HandlePayload(BufferPayload bp) {
            if (bp.Key == KeybdKeys.NO_SUCH_KEY) {
                // dump over to buffer
            } else {
                Logger.Log(NetClientID + ":" + bp.Key);
            }
        }
    }
}
