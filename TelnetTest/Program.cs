using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelnetTest {
    public static class Program {
        public static void Main(String[] argv) {
            Server server = new Server();
            server.Start();
        }
    }
}
