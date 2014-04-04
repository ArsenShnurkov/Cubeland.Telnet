using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Cubeland.Telnet {
    public static class TelnetDecorator {

        static Dictionary<String, String> CodeList = new Dictionary<string, string> {
            {"<@off@>",         Vt100Codes.CharModeOff},
            {"<@bright@>",      Vt100Codes.CharBright},
            {"<@bold@>",        Vt100Codes.CharBright},
            {"<@dim@>",         Vt100Codes.CharDim},
            {"<@blink@>",       Vt100Codes.CharBlink},
            {"<@ul@>",          Vt100Codes.CharUL},
            {"<@black@>",       Vt100Codes.CharFgBlack},
            {"<@red@>",         Vt100Codes.CharFgRed},
            {"<@green@>",       Vt100Codes.CharFgGreen},
            {"<@yellow@>",      Vt100Codes.CharFgYellow},
            {"<@blue@>",        Vt100Codes.CharFgBlue},
            {"<@magenta@>",     Vt100Codes.CharFgMagenta},
            {"<@cyan@>",        Vt100Codes.CharFgCyan},
            {"<@white@>",       Vt100Codes.CharFgWhite},
            {"<@bgblack@>",     Vt100Codes.CharBgBlack},
            {"<@bgred@>",       Vt100Codes.CharBgRed},
            {"<@bggreen@>",     Vt100Codes.CharBgGreen},
            {"<@bgyellow@>",    Vt100Codes.CharBgYellow},
            {"<@bgblue@>",      Vt100Codes.CharBgBlue},
            {"<@bgmagenta@>",   Vt100Codes.CharBgMagenta},
            {"<@bgcyan@>",      Vt100Codes.CharBgCyan},
            {"<@bgwhite@>",     Vt100Codes.CharBgWhite}
        };

        static string MultipleStrReplace(string text, Dictionary<String, String> replacements) {
            return Regex.Replace(text,
                                    "(" + String.Join("|", replacements.Keys.ToArray()) + ")",
                                    delegate(Match m) { return replacements[m.Value]; }
                                    );
        }

        public static byte[] Decorate(byte[] input) {
            List<Byte> ByteList = new List<byte>();
            // <@ ... @>
            String builder = "";
            bool InTag = false;
            for (int xx = 0; xx < input.Length; xx++) {
                if (InTag) {
                    if (input[xx] == 64 && xx < input.Length - 1) {
                        // check to see if we're leaving the tag
                        if (input[xx + 1] == 62) {
                            builder = builder + @"@>";
                            xx++;
                            // replace with tag here
                            Console.WriteLine("Found tag:" + builder.ToString());
                            InTag = false;
                        }
                    } else {
                        builder = builder + Convert.ToChar(input[xx]);
                    }
                } else if (input[xx] == 60 && xx < input.Length - 1) {
                    // check to see if we're entering the tag
                    if (input[xx + 1] == 64) {
                        InTag = true;
                        builder = "<@";
                        xx++;
                    } else {
                        ByteList.Add(input[xx]);
                    }
                } else {
                    ByteList.Add(input[xx]);
                }
            }
            return (ByteList.ToArray<byte>());
        }

        public static String Decorate(String input) {

            input = MultipleStrReplace(input, CodeList);

            // strip out any unrecognized control characters
            input = Regex.Replace(input, "<@.*@>", "");

            return (input + Vt100Codes.CharModeOff);
        }

        public static String Clean(String input) {
            return (Regex.Replace(input, "[\x01-\x1F]", ""));
        }

        public static string Wrap(string text, int maxLength) {
            
            text = text.Replace(",", ", ");
            text = text.Replace(";", "; ");
            text = text.Replace(" ", " ");
            text = text.Replace(".", ". ");
            text = text.Replace("<", " <");
            text = text.Replace(">", "> ");

            // BUG: need to handle multi-line strings by breaking all this up
            String[] LineStrings = text.Split('\n');
            // ...

            string[] Words = text.Split(' ');
            int currentLineLength = 0;
            List<String> Lines = new List<String>(text.Length / maxLength);
            string currentLine = "";

            bool InTag = false;

            foreach (string currentWord in Words) {
                if (currentWord.Length > 0) {
                    if (currentWord.Length > 2 && currentWord.Substring(0, 2) == @"<@") {
                        InTag = true;
                    }

                    if (InTag) {
                        currentLine += " " + currentWord;
                        InTag = false;
                    } else {
                        if (currentLineLength + currentWord.Length + 1 < maxLength) {
                            currentLine += " " + currentWord;
                            currentLineLength += (currentWord.Length + 1);
                        } else {
                            Lines.Add(currentLine);
                            currentLine = currentWord;
                            currentLineLength = currentWord.Length;
                        }
                    }
                }
            }
            if (currentLine != "") {
                Lines.Add(currentLine);
                currentLineLength = 0;
            }
            string[] textLinesStr = new string[Lines.Count];
            Lines.CopyTo(textLinesStr, 0);
            String str = "";
            int x = 0;
            foreach (String s in textLinesStr) {
                if (x == 0) {
                    str = s;
                } else {
                    str = str + System.Environment.NewLine + s;
                }
                x++;
            }
            str = Regex.Replace(str, @">\s+", @">");
            str = Regex.Replace(str, @"\s+<", @"<");
            str = Regex.Replace(str, @">", @"> ");
            // fix spaces in front of nonalphanumeric characters
            str = Regex.Replace(str, @"> ([\,,\.,\;,\:])", @">$1");
            return (str);
        }

        public static string[] WrapToArray(string longtext, int maxLength) {
            maxLength--;
            longtext = longtext.Replace(",", ", ");
            longtext = longtext.Replace(";", "; ");
            longtext = longtext.Replace(" ", " ");
            longtext = longtext.Replace(".", ". ");
            longtext = longtext.Replace("<", " <");
            longtext = longtext.Replace(">", "> ");

            String[] LineStrings = longtext.Split('\n');
            List<String[]> longTextLineStr = new List<string[]>();

            foreach (String text in LineStrings) {
                string[] Words = text.Split(' ');
                int currentLineLength = 0;
                List<String> Lines = new List<String>(text.Length / maxLength);
                string currentLine = "";

                bool InTag = false;

                foreach (string currentWord in Words) {
                    if (currentWord.Length > 0) {
                        if (currentWord.Length > 2 && currentWord.Substring(0, 2) == @"<@") {
                            InTag = true;
                        }

                        if (InTag) {
                            currentLine += " " + currentWord;
                            InTag = false;
                        } else {
                            if (currentLineLength + currentWord.Length + 1 < maxLength) {
                                currentLine += " " + currentWord;
                                currentLineLength += (currentWord.Length + 1);
                            } else {
                                Lines.Add(currentLine);
                                currentLine = currentWord;
                                currentLineLength = currentWord.Length;
                            }
                        }
                    }

                }
                if (currentLine != "") {
                    Lines.Add(currentLine);
                }
                string[] textLinesStr = new string[Lines.Count];
                Lines.CopyTo(textLinesStr, 0);
                String str = "";
                for (int yy = 0; yy < textLinesStr.Length; yy++) {
                    textLinesStr[yy] = Regex.Replace(textLinesStr[yy], @">\s+", @">");
                    textLinesStr[yy] = Regex.Replace(textLinesStr[yy], @"\s+<", @"<");
                    textLinesStr[yy] = Regex.Replace(textLinesStr[yy], @">", @"> ");
                    // fix spaces in front of nonalphanumeric characters
                    textLinesStr[yy] = Regex.Replace(textLinesStr[yy], @"> ([\,,\.,\;,\:])", @">$1");
                }

                longTextLineStr.Add(textLinesStr);
            }

            List<String> sarr = new List<String>();

            foreach (String[] sa in longTextLineStr) {
                foreach (String s in sa) {
                    sarr.Add(s);
                }
            }

            return (sarr.ToArray());
        }

    }
}
