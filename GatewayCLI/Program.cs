/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of GatewayCLI <https://github.com/OpenChargingCloud/GatewayCLI>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.Gateway.CommandLine;
using cloud.charging.open.Gateway.Configuration;
using cloud.charging.open.Gateway.Logging;

#endregion

namespace cloud.charging.open.Gateway
{

    /// <summary>
    /// One gateway, with its web interface, until 'quit' or Ctrl+C.
    /// </summary>
    /// <remarks>
    /// The switches here are this program's vocabulary and nothing else: what
    /// a gateway is, and what it does, lives in the Gateway library. The one
    /// thing this file decides on its own is what to print when the gateway is
    /// up, because that is the one moment somebody is reading a console rather
    /// than the web interface.
    /// </remarks>
    public class Program
    {

        #region (private static) TryTakeValue(Arguments, ref Index, out Value)

        private static Boolean TryTakeValue(String[]     Arguments,
                                            ref Int32    Index,
                                            out String?  Value)
        {

            if (Index + 1 < Arguments.Length && !Arguments[Index + 1].StartsWith("--"))
            {
                Value = Arguments[++Index];
                return true;
            }

            Value = null;
            return false;

        }

        #endregion

        #region (private static) RepositoryRoot()

        /// <summary>
        /// The directory holding GatewayCLI.slnx, looked up from the binary and
        /// from the current directory; the current directory when neither
        /// leads to it.
        /// </summary>
        /// <remarks>
        /// The accounts, the configuration and the logs default to a place
        /// below it, so that they do not end up in bin/ - where the next
        /// "dotnet clean" would take this gateway's accounts with it.
        /// </remarks>
        private static String RepositoryRoot()
        {

            foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
            {

                var directory = new DirectoryInfo(start);

                while (directory is not null)
                {

                    if (File.Exists(Path.Combine(directory.FullName, "GatewayCLI.slnx")))
                        return directory.FullName;

                    directory = directory.Parent;

                }

            }

            return Environment.CurrentDirectory;

        }

        #endregion

        #region (private static) PrintUsage()

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: GatewayCLI [--port <number>] [--any] [--frontend <dist directory>]");
            Console.WriteLine("                  [--accounts <dir>] [--config <file>] [--verbose | --quiet] [--no-trace]");
            Console.WriteLine("                  [--log-file <dir>] [--no-log-file]");
            Console.WriteLine();
            Console.WriteLine("Web interface:");
            Console.WriteLine($"  --port <number>   TCP port to listen on (default: {Gateway.DefaultHTTPPort})");
            Console.WriteLine("  --any             listen on all addresses instead of 127.0.0.1");
            Console.WriteLine("  --frontend <dir>  serve the web interface from a directory on disk instead of the");
            Console.WriteLine("                    bundle embedded in the assembly - use it together with");
            Console.WriteLine("                    'npm run watch' in libs/Gateway/Gateway/Frontend");
            Console.WriteLine();
            Console.WriteLine("Accounts:");
            Console.WriteLine($"  --accounts <dir>    where the accounts live (default: {Gateway.DefaultAccountsPath}/ below the");
            Console.WriteLine("                      repository root). Without it a password is made up at the");
            Console.WriteLine($"                      first start for the user '{Gateway.DefaultAdminUser}' and shown once.");
            Console.WriteLine();
            Console.WriteLine("Configuration:");
            Console.WriteLine($"  --config <file>   where the name servers and the time servers of this gateway live");
            Console.WriteLine($"                    (default: {GatewayConfigFile.DefaultFileName} below the repository root). Without");
            Console.WriteLine("                    the file the gateway runs on the system defaults; the");
            Console.WriteLine("                    Configuration pages of the web interface write it, and every");
            Console.WriteLine("                    change there takes effect at once.");
            Console.WriteLine();
            Console.WriteLine("Log:");
            Console.WriteLine("  -v, --verbose     write every entry to the console, down to the debug ones");
            Console.WriteLine("  -q, --quiet       write only warnings and worse");
            Console.WriteLine("      --no-trace    do not pick up what the libraries below write with DebugX");
            Console.WriteLine();
            Console.WriteLine("Whatever the console shows, the web interface shows the whole log under 'Logs'.");
            Console.WriteLine();
            Console.WriteLine($"  --log-file <dir>  where the log files go (default: {Gateway.DefaultLogPath}/ below the repository");
            Console.WriteLine("                    root). One file per day, every entry down to the debug ones, and");
            Console.WriteLine("                    nothing is ever deleted.");
            Console.WriteLine("      --no-log-file do not write one. Then what the console did not show, and what");
            Console.WriteLine("                    falls out of the web interface's last 2000 entries, is gone.");
            Console.WriteLine();
            Console.WriteLine("Once it is up, the console is a prompt: 'help' lists what can be typed there,");
            Console.WriteLine("Tab completes it, and 'quit' or Ctrl+C stops the gateway. Started where there is");
            Console.WriteLine("no terminal on the input - from a script, under a service manager, in CI - there");
            Console.WriteLine("is no prompt and it simply runs.");
        }

        #endregion


        public static async Task<Int32> Main(String[] Arguments)
        {

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            #region Arguments

            IPPort?  port           = null;
            var      anyAddress     = false;
            String?  frontendDir    = null;
            String?  accountsPath   = null;
            String?  logPath        = null;
            var      noLogFile      = false;
            String?  configFilePath = null;
            var      verbose        = false;
            var      quiet          = false;
            var      noTrace        = false;

            for (var i = 0; i < Arguments.Length; i++)
            {
                switch (Arguments[i])
                {

                    case "--port":
                        if (i + 1 < Arguments.Length && UInt16.TryParse(Arguments[i + 1], out var parsedPort))
                        {
                            port = IPPort.Parse(parsedPort);
                            i++;
                        }
                        else
                        {
                            Console.Error.WriteLine("Missing or invalid port number after --port!");
                            return 2;
                        }
                        break;

                    case "--any":
                        anyAddress = true;
                        break;

                    case "--frontend":
                        if (!TryTakeValue(Arguments, ref i, out frontendDir))
                        {
                            Console.Error.WriteLine("Missing directory after --frontend!");
                            return 2;
                        }
                        break;

                    case "--accounts":
                        if (!TryTakeValue(Arguments, ref i, out accountsPath))
                        {
                            Console.Error.WriteLine("Missing directory after --accounts!");
                            return 2;
                        }
                        break;

                    case "--log-file":
                        if (!TryTakeValue(Arguments, ref i, out logPath))
                        {
                            Console.Error.WriteLine("Missing directory after --log-file!");
                            return 2;
                        }
                        break;

                    case "--no-log-file":
                        noLogFile = true;
                        break;

                    case "--config":
                        if (!TryTakeValue(Arguments, ref i, out configFilePath))
                        {
                            Console.Error.WriteLine("Missing file after --config!");
                            return 2;
                        }
                        break;

                    case "-v":
                    case "--verbose":
                        verbose = true;
                        break;

                    case "-q":
                    case "--quiet":
                        quiet = true;
                        break;

                    case "--no-trace":
                        noTrace = true;
                        break;

                    case "-h":
                    case "--help":
                        PrintUsage();
                        return 0;

                    default:
                        Console.Error.WriteLine($"Unknown argument '{Arguments[i]}'!");
                        PrintUsage();
                        return 2;

                }
            }

            if (verbose && quiet)
            {
                Console.Error.WriteLine("--verbose and --quiet ask for opposite things!");
                return 2;
            }

            #endregion

            #region Where the web interface comes from

            // A directory given on the command line wins, so that
            // "npm run watch" beside a running gateway shows up in the browser
            // on a reload, without rebuilding the C# side.
            IStaticContentSource? frontend = null;

            if (frontendDir is not null)
            {

                if (!Directory.Exists(frontendDir))
                {
                    Console.Error.WriteLine($"The frontend directory '{frontendDir}' does not exist!");
                    return 2;
                }

                frontend = new FileSystemContentSource(frontendDir);

            }

            #endregion

            #region The gateway

            Gateway gateway;

            try
            {
                gateway = new Gateway(

                              HTTPHostname:     anyAddress
                                                    ? IPvXAddress.Any
                                                    : IPv4Address.Localhost,

                              HTTPPort:         port,

                              AccountsPath:     accountsPath ?? Path.Combine(RepositoryRoot(), Gateway.DefaultAccountsPath),

                              ConfigFile:       new GatewayConfigFile(
                                                    configFilePath ?? Path.Combine(RepositoryRoot(), GatewayConfigFile.DefaultFileName)
                                                ),

                              Frontend:         frontend,

                              ConsoleLogLevel:  verbose ? LogLevel.Debug
                                                    : quiet ? LogLevel.Warning
                                                    : LogLevel.Info,

                              // On unless it is switched off. A console nobody
                              // was watching kept nothing, and the log a
                              // browser shows goes with the process - so the
                              // one place an afternoon's question can still be
                              // answered from is a file.
                              LogPath:          noLogFile
                                                    ? null
                                                    : logPath ?? Path.Combine(RepositoryRoot(), Gateway.DefaultLogPath),

                              BridgeDebugLog:   !noTrace

                          );
            }
            catch (Exception e)
            {

                Console.Error.WriteLine($"The gateway could not be set up: {e.Message}");

                // A gateway that does not come up at all is the one moment the
                // stack trace is worth more than a tidy console.
                if (verbose)
                    Console.Error.WriteLine(e);

                return 1;

            }

            #endregion

            await using (gateway)
            {

                try
                {
                    await gateway.Start();
                }
                catch (PortUnavailableException problem)
                {

                    // The operating system's own words for a port in use are in
                    // German on a German Windows and name the port nowhere, so
                    // the exception says it in words of its own - and what the
                    // usual answer is.
                    Console.Error.WriteLine($"The gateway could not start: {problem.Message}.");
                    Console.Error.WriteLine("Another copy of this gateway already running is the usual answer. " +
                                            "Stop it, or give this one another port with --port <number>.");

                    if (verbose)
                        Console.Error.WriteLine(problem);

                    return 1;

                }

                #region What somebody who just started this needs to know

                Console.WriteLine();
                Console.WriteLine($"  web interface  {gateway.WebInterfaceURL}");
                Console.WriteLine($"  JSON API       {gateway.APIURL}v1/status");
                Console.WriteLine($"  event stream   {gateway.APIURL}v1/events");
                Console.WriteLine($"  frontend from  {gateway.Frontend.Description}");

                // What this binary actually is, for whoever reads a bug report.
                // Read out of the assemblies rather than handed in on the command
                // line: the command line describes the working tree at startup,
                // these describe the trees each part was compiled from, and after
                // a checkout without a rebuild those are not the same answer.
                var builtFrom = BuiltFrom.Repositories.ToArray();

                if (builtFrom.Length > 0)
                {

                    // One line each, and the whole hash. This is meant to be read
                    // out of a bug report and pasted into a checkout, and an
                    // abbreviation is a thing somebody then has to guess the rest
                    // of. Where two repositories share a directory name, the
                    // assembly is named as well, so that the lines stay apart.
                    String Label(LoadedAssembly repository)
                        => builtFrom.Count(other => other.Repository == repository.Repository) > 1
                               ? $"{repository.Repository} ({repository.Name})"
                               : repository.Repository!;

                    var width = builtFrom.Max(repository => Label(repository).Length);

                    for (var i = 0; i < builtFrom.Length; i++)
                        Console.WriteLine((i == 0 ? "  built from     " : "                 ") +
                                          Label(builtFrom[i]).PadRight(width) +
                                          "  " +
                                          builtFrom[i].Commit);

                }

                Console.WriteLine($"  accounts       {gateway.ExtAPI.Users.Count()} user(s) in {gateway.AccountsPath}");
                Console.WriteLine($"  sign in at     {gateway.WebInterfaceURL}{Gateway.ExtAPIPath.ToString().Trim('/')}/login");
                Console.WriteLine($"  configuration  {gateway.ConfigFile.Path}");
                Console.WriteLine($"  name servers   {(gateway.DNSEnabled ? String.Join(", ", gateway.DNSClient.DNSServers) : "switched off")}");

                #region The time servers

                var bands = gateway.TimeSources.Bands();
                var asked = bands.SelectMany(band => band).ToArray();

                if (asked.Length <= 1)
                    Console.WriteLine($"  time server    {gateway.NTSClient.Hostname}{(gateway.NTSEnabled ? "" : " (switched off)")}");

                else
                {

                    // One line per band, because a band is the unit that is
                    // asked at once - putting two bands on one line would read
                    // as six equal servers when it is two and then four.
                    for (var i = 0; i < bands.Count; i++)
                        Console.WriteLine((i == 0 ? "  time servers   " : "                 ") +
                                          String.Join(", ", bands[i].Select(source => source.Hostname.Trimmed)) +
                                          (bands.Count > 1 ? $"   (priority {bands[i][0].Priority})" : ""));

                    Console.WriteLine($"                 at least {gateway.TimeSources.MinServers} of them must answer" +
                                      (gateway.NTSEnabled ? "" : " - and NTS is switched off"));

                }

                #endregion

                if (gateway.GeneratedPassword is not null)
                {
                    Console.WriteLine();
                    Console.WriteLine("  ┌─ First start: there were no accounts, so one was made up for you ─────────");
                    Console.WriteLine($"  │  user      {Gateway.DefaultAdminUser}");
                    Console.WriteLine($"  │  password  {gateway.GeneratedPassword}");
                    // Named rather than called "a hash", and read from the
                    // implementation rather than typed here, so the box cannot
                    // end up describing a scheme this gateway no longer uses.
                    Console.WriteLine($"  │  It is shown here once and kept only as a {SecurePassword.PBKDF2SHA256} hash");
                    Console.WriteLine($"  │  over {SecurePassword.DefaultIterations} iterations. Write it down.");
                    Console.WriteLine("  └───────────────────────────────────────────────────────────────────────────");
                }

                Console.WriteLine();

                #endregion

                #region The command line, until 'quit' or Ctrl+C

                // Whether anybody can type here at all. Started from a script,
                // from a service manager or in CI, this process has no terminal
                // on its input and Console.ReadKey throws rather than waiting -
                // and there would be nobody to type anyway. Then the gateway
                // simply runs, and the web interface is how it is spoken to.
                var canBeTypedAt = !Console.IsInputRedirected;

                Console.WriteLine(canBeTypedAt
                                      ? "Type 'help' for what can be typed here, 'quit' or Ctrl+C to stop."
                                      : "Press Ctrl+C to stop. (No terminal on the input here, so nothing to type at.)");
                Console.WriteLine();

                var stopped = new TaskCompletionSource();

                // Ctrl+C still means stop. The command line adds a handler of
                // its own for it, which cancels whatever command is running;
                // both fire, and that is the intended reading of Ctrl+C -
                // abandon what is running and shut the gateway down. 'quit' is
                // the same thing said politely.
                Console.CancelKeyPress += (_, e) => {
                    e.Cancel = true;
                    stopped.TrySetResult();
                };

                if (canBeTypedAt)
                {

                    // From here two things write on one screen: this command
                    // line, and the gateway's log from whichever thread did the
                    // thing it is reporting. So the log stops writing of its own
                    // accord and asks the command line for the screen instead -
                    // which takes the half-typed command off it, writes the
                    // entry whole, and puts the command back with the cursor
                    // where it was.
                    var cli = new GatewayCLI(gateway);

                    gateway.ShareConsoleWith(cli.WriteBlock);

                    // On a thread of its own, because Console.ReadKey blocks the
                    // one it is called on: awaited directly, the command line
                    // would keep this thread inside ReadKey and Ctrl+C would
                    // have nobody left to wake.
                    await Task.WhenAny(stopped.Task, Task.Run(cli.Run));

                }

                else
                    await stopped.Task;

                #endregion

            }

            return 0;

        }

    }

}
