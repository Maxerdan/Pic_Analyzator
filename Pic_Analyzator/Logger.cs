using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;

namespace Pic_Analyzator
{
    public class Logger
    {
        private Stopwatch Watch { get; set; }

        public List<string> Measures { get; set; }

        public Logger()
        {
            Measures = new List<string>();
            Watch = Stopwatch.StartNew();
        }

        public void Log()
        {
            Measures.Add((Watch.ElapsedMilliseconds / 1000.0).ToString());
        }

        public void ShowMeasures()
        {
            Watch.Stop();

            var message = "";
            foreach (var measure in Measures)
            {
                message = measure + "\n";
            }
            message.Remove(message.LastIndexOf("\n"), 1);

            message = String.Join("\n", Measures);

            MessageBox.Show(message);
        }
    }
}
