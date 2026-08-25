using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace WindowsGSM.GameServer
{
    class INSS
    {
        private readonly Functions.ServerConfig _serverData;

        public string Error;
        public string Notice;

        public const string FullName = "Insurgency: Sandstorm Dedicated Server";

        // Correct starting path
        public string StartPath = @"Insurgency\Binaries\Win64\InsurgencyServer-Win64-Shipping.exe";

        public bool AllowsEmbedConsole = true;

        // Sandstorm using UT3 Query
        public int PortIncrements = 2;
        public dynamic QueryMethod = new Query.UT3();

        // StandardValues
        public string Port = "27102";
        public string QueryPort = "27131";
        public string Defaultmap = "Farmhouse";
        public string Maxplayers = "28";

        // Default Scenario
        public string DefaultScenario = "Scenario_Farmhouse_Frontline";

        // Mutators (Default is empty)
        public string DefaultMutators = "";

        // Tokens (optional)
        public string SecurityCode = "";
        public string GameStatsToken = "";
        public string GSLTToken = "";

        public string AppId = "581330";

        public INSS(Functions.ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            // Sandstorm no needs CFG-Files
        }

        public async Task<Process> Start()
        {
            string exePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);

            if (!File.Exists(exePath))
            {
                Error = $"{Path.GetFileName(exePath)} not found ({exePath})";
                return null;
            }

            // -----------------------------
            // PARAMETER ENGINE
            // -----------------------------

            string map = string.IsNullOrWhiteSpace(_serverData.ServerMap)
                ? Defaultmap
                : _serverData.ServerMap;

            string scenario = string.IsNullOrWhiteSpace(_serverData.ServerScenario)
                ? DefaultScenario
                : _serverData.ServerScenario;

            string mutators = string.IsNullOrWhiteSpace(_serverData.ServerAdditional)
                ? DefaultMutators
                : _serverData.ServerAdditional;

            string param = $"{map}?Scenario={scenario}";

            if (!string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer))
                param += $"?MaxPlayers={_serverData.ServerMaxPlayer}";
            else
                param += $"?MaxPlayers={Maxplayers}";

            if (!string.IsNullOrWhiteSpace(mutators))
                param += $"?Mutators={mutators}";

            // Ports
            param += $" -Port={_serverData.ServerPort}";
            param += $" -QueryPort={_serverData.ServerQueryPort}";

            // MultiHome
            if (!string.IsNullOrWhiteSpace(_serverData.ServerIP))
                param += $" -MultiHome={_serverData.ServerIP}";

            // Hostname
            if (!string.IsNullOrWhiteSpace(_serverData.ServerName))
                param += $" -hostname=\"{_serverData.ServerName}\"";

            // Tokens
            if (!string.IsNullOrWhiteSpace(SecurityCode))
                param += $" -SecurityCode={SecurityCode}";

            if (!string.IsNullOrWhiteSpace(GameStatsToken))
                param += $" -GameStatsToken={GameStatsToken}";

            if (!string.IsNullOrWhiteSpace(GSLTToken))
                param += $" -GSLTToken={GSLTToken}";

            // Logging
            param += " -log";

            // Mods
            param += " -mods";

            // -----------------------------
            // PROCESS START
            // -----------------------------

            Process p;

            if (!AllowsEmbedConsole)
            {
                p = new Process
                {
                    StartInfo =
                    {
                        FileName = exePath,
                        Arguments = param,
                        WindowStyle = ProcessWindowStyle.Minimized,
                        UseShellExecute = false
                    },
                    EnableRaisingEvents = true
                };
                p.Start();
            }
            else
            {
                p = new Process
                {
                    StartInfo =
                    {
                        FileName = exePath,
                        Arguments = param,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    },
                    EnableRaisingEvents = true
                };

                var serverConsole = new Functions.ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }

            return p;
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                if (p.StartInfo.CreateNoWindow)
                    p.Kill();
                else
                    p.CloseMainWindow();
            });
        }

        public async Task<Process> Install()
        {
            var steamCMD = new Installer.SteamCMD();
            Process p = await steamCMD.Install(_serverData.ServerID, string.Empty, AppId);
            Error = steamCMD.Error;
            return p;
        }

        public async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(_serverData.ServerID, AppId, validate, custom: custom);
            Error = error;
            return p;
        }

        public bool IsInstallValid()
        {
            return File.Exists(
                Functions.ServerPath.GetServersServerFiles(
                    _serverData.ServerID,
                    StartPath
                )
            );
        }

        public bool IsImportValid(string path)
        {
            string exePath = Path.Combine(path, @"Insurgency\Binaries\Win64\InsurgencyServer-Win64-Shipping.exe");
            Error = $"Invalid Path! Fail to find {Path.GetFileName(exePath)}";
            return File.Exists(exePath);
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }
    }
}
