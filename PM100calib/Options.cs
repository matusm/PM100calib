using CommandLine;
using CommandLine.Text;

namespace PM100calib
{
    public class Options
    { 
    [Option('n', "number", DefaultValue = 10, HelpText = "Number of samples.")]
    public int MaximumSamples { get; set; }

    [Option("comment", DefaultValue = "", HelpText = "User supplied comment string.")]
    public string UserComment { get; set; }

    [Option("logfile", DefaultValue = "PM100calib", HelpText = "Log and CSV base file name.")]
    public string LogFileName { get; set; }

    [HelpOption]
    public string GetUsage()
    {
        string AppName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        string AppVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

        HelpText help = new HelpText
        {
            Heading = new HeadingInfo($"{AppName}, version {AppVer}"),
            Copyright = new CopyrightInfo("Michael Matus", 2022),
            AdditionalNewLineAfterOption = false,
            AddDashesToOption = true
        };
        string preamble = "Program to calibrate a Thorlabs PM100D optical power meter. It is controlled via its USB interface. " +
            "Measurement results are logged in a file. Take care: CSV files are overwritten without warning.";
        help.AddPreOptionsLine(preamble);
        help.AddPreOptionsLine("");
        help.AddPreOptionsLine($"Usage: {AppName} [options]");
        help.AddPostOptionsLine("");
        help.AddOptions(this);

        return help;
    }
}

}
