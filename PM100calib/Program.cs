using At.Matus.StatisticPod;
using Bev.Instruments.Thorlabs.PM;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;


namespace PM100calib
{
    class Program
    {
        static int Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            string appName = Assembly.GetExecutingAssembly().GetName().Name;
            var appVersion = Assembly.GetExecutingAssembly().GetName().Version;
            string appVersionString = $"{appVersion.Major}.{appVersion.Minor}";
            Options options = new Options();
            if (!CommandLine.Parser.Default.ParseArgumentsStrict(args, options))
                Console.WriteLine("*** ParseArgumentsStrict returned false");

            DisplayOnly("Searching for devices ...");
            DiscoverPM dpm = new DiscoverPM(); // time expensive!
            if (dpm.NumberOfDevices == 0)
            {
                DisplayOnly("No powermeter found!");
                return 1;
            }
            DisplayOnly($"Number of detected devices: {dpm.NumberOfDevices}");
            foreach (var s in dpm.NamesOfDevices)
                DisplayOnly($"-  {s}");
            DisplayOnly("");

            DateTime timeStamp = DateTime.UtcNow;
            ThorlabsPM pm = new ThorlabsPM(dpm.FirstDevice);
            StreamWriter logWriter = new StreamWriter(options.LogFileName+".log", true);
            StreamWriter csvWriter = new StreamWriter(options.LogFileName+".csv", false);
            StatisticPod stpCurrent = new StatisticPod("Current in A");
            
            if (options.MaximumSamples < 2) options.MaximumSamples = 2;
            if (string.IsNullOrWhiteSpace(options.UserComment))
            {
                options.UserComment = "---";
            }

            pm.SetMeasurementRange(MeasurementRange.Range03);

            DisplayOnly("");
            LogOnly(fatSeparator);
            DisplayOnly($"Application:     {appName} {appVersionString}");
            LogOnly($"Application:     {appName} {appVersion}");
            LogAndDisplay($"StartTimeUTC:    {timeStamp:dd-MM-yyyy HH:mm}");
            LogAndDisplay($"InstrumentManu:  {pm.InstrumentManufacturer}");
            LogAndDisplay($"InstrumentType:  {pm.InstrumentType}");
            LogAndDisplay($"InstrumentSN:    {pm.InstrumentSerialNumber}");
            LogAndDisplay($"ResourceName:    {pm.DevicePort}");
            LogAndDisplay($"Samples (n):     {options.MaximumSamples}");
            LogAndDisplay($"Comment:         {options.UserComment}");
            LogOnly(fatSeparator);
            DisplayOnly("");
            CsvLog(CsvHeader());

            int measurementIndex = 0;
            bool shallLoop = true;
            while (shallLoop)
            {
                DisplayOnly("press any key to start a measurement - 'q' to quit, arrow keys to change range");
                ConsoleKeyInfo cki = Console.ReadKey(true);
                switch (cki.Key)
                {
                    case ConsoleKey.Q:
                        shallLoop = false;
                        DisplayOnly("bye.");
                        break;
                    case ConsoleKey.DownArrow:
                    case ConsoleKey.PageDown:
                        var r = pm.GetMeasurementRange();
                        pm.SetMeasurementRange(r.Decrement());
                        DisplayCurrentRange();
                        break;
                    case ConsoleKey.UpArrow:
                    case ConsoleKey.PageUp:
                        var s = pm.GetMeasurementRange();
                        pm.SetMeasurementRange(s.Increment());
                        DisplayCurrentRange();
                        break;
                    default:
                        int iterationIndex = 0;
                        measurementIndex++;
                        DisplayOnly("");
                        DisplayOnly($"Measurement #{measurementIndex} at {pm.GetMeasurementRange()}");
                        RestartValues();
                        timeStamp = DateTime.UtcNow;

                        while (iterationIndex < options.MaximumSamples)
                        {
                            iterationIndex++;
                            double current = pm.GetCurrent();
                            UpdateValues(current);
                            DisplayOnly($"{iterationIndex,4}:  {current * 1e9:F3} nA");
                        }

                        DisplayOnly("");
                        LogOnly($"Measurement number:   {measurementIndex} ({pm.GetMeasurementRange()})");
                        LogOnly($"Triggered at:         {timeStamp:dd-MM-yyyy HH:mm:ss}");
                        //LogAndDisplay($"current:              {stpCurrent.AverageValue.ToString("0.000E0")} ± {stpCurrent.StandardDeviation.ToString("0.000E0")} A");
                        LogAndDisplay($"Actual sample size:   {stpCurrent.SampleSize}");
                        LogAndDisplay($"Current:              {stpCurrent.AverageValue * 1e9:F3} ± {stpCurrent.StandardDeviation * 1e9:F3} nA");
                        LogOnly(thinSeparator);
                        DisplayOnly("");
                        CsvLog(CsvLine(measurementIndex));
                        break;
                }
            }

            logWriter.Close();
            csvWriter.Close();
            return 0;

            /***************************************************/
            void LogAndDisplay(string line)
            {
                DisplayOnly(line);
                LogOnly(line);
            }
            /***************************************************/
            void LogOnly(string line)
            {
                logWriter.WriteLine(line);
                logWriter.Flush();
            }
            /***************************************************/
            void DisplayOnly(string line)
            {
                Console.WriteLine(line);
            }
            /***************************************************/
            void RestartValues()
            {
                stpCurrent.Restart();
            }
            /***************************************************/
            void UpdateValues(double x)
            {
                if (double.IsInfinity(x)) 
                    x = double.NaN;
                stpCurrent.Update(x);
            }
            /***************************************************/
            void DisplayCurrentRange()
            {
                DisplayOnly("");
                DisplayOnly($"Current measurement range: {pm.GetMeasurementRange()}");
                DisplayOnly("");
            }
            /***************************************************/
            string CsvHeader() => $"measurement number, range, sample size, specification (A), measured current (A), standard deviation (A), test current (A), standard uncertainty (A)";
            /***************************************************/
            string CsvLine(int index) => $"{index}, {pm.GetMeasurementRange()}, {stpCurrent.SampleSize}, {pm.GetSpecification(stpCurrent.AverageValue, pm.GetMeasurementRange())}, {stpCurrent.AverageValue}, {stpCurrent.StandardDeviation}, {"[TestCurrent]"}, {"[u(TestCurrent)]"}";
            /***************************************************/
            void CsvLog(string line)
            {
                csvWriter.WriteLine(line);
                csvWriter.Flush();
            }
            /***************************************************/

        }

        private static readonly string fatSeparator = new string('=', 80);
        private static readonly string thinSeparator = new string('-', 80);
    }
}
