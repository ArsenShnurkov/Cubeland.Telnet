using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cubeland.Telnet {

    class MenuCell {
        int Top;
        int Left;
        int Width;
        int Height;
        TelnetMenu Menu;
        Stack<TelnetMenu> MenuStack;
    }


    public class MenuGrid {
        Dictionary<String, MenuCell> Cells;
        List<String> CurrentScreen;
        List<String> NextScreen;

        public void AddNewCell(String s) {
        }

        public void AddNewCell(String s, TelnetMenu m) {
        }
    
    }
}
