using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace Pic_Analyzator
{
    public class Logger
    {
        private Form form;
        private Stopwatch Watch { get; set; }
        public List<string> Measures { get; set; }
        private int LogCount { get; set; }
        const int LogsMax = 5;

        public Logger(Form form)
        {
            LogCount = 0;
            this.form = form;
        }

        // method to log caption
        public void Log(string text)
        {
            if (LogCount == 0)
                StartMeasures();

            LogCount++;
            form.Invoke(new Action(() => { form.Text = $"({LogCount}/{LogsMax}) " + text; }));
            Measure();

            if (LogCount == LogsMax)
                ShowMeasures();
        }

        public void StartMeasures()
        {
            Measures = new List<string>();
            Watch = Stopwatch.StartNew();
        }

        public void Measure()
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
