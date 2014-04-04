using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cubeland.Telnet {
    public class TelnetMenu {

        // delegates to do stuff
        public delegate void MenuCharCallback(Char c);
        public delegate void MenuKeyCallback(KeybdKeys k);

        public int Width;
        public int Height;
        List<MenuItem> HeadlineList;
        List<MenuItem> ItemList;

        public TelnetMenu() {
            HeadlineList = new List<MenuItem>();
            ItemList = new List<MenuItem>();
        }

        public void AddHeadline(String HedText){
        }

        public void AddHeadline(String HedText, int loc) {
        }

        public void AddHeadline(String HedText, String FormatText) {
        }

        public void AddHeadline(String HedText, String FormatText, String HelpText) {
        }

        public void AddHeadline(String HedText, String FormatText, String HelpText, int Loc) {
        }

        public void AddItem(String MenuText) {
        }

        public void AddItem(String MenuText, String FormatText) {
        }

        public void AddItem(String MenuText, String FormatText, String HelpText) {
        }

        public void AddItem(String MenuText, String FormatText, String HelpText, ItemValueTypes MenuType) {
        }

        public void AddItem(String MenuText, String FormatText, String HelpText, ItemValueTypes MenuType, Object DefaultValue) {
        }

        public void AddItem(String MenuText, String FormatText, String HelpText, ItemValueTypes MenuType, Object DefaultValue, int MaxVal) {
        }

        public String DisplayMenu() {
            throw new NotImplementedException();
        }
    }
}
