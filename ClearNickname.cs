using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ClearNickname
{
    public class ClearNickname : BasePlugin
    {
        public override string ModuleName => "ClearNickname";
        public override string ModuleVersion => "v1.0";
        public override string ModuleAuthor => "E!N";

        private readonly List<string> badWords = [];
        private string? prefix;

        public override void Load(bool hotReload)
        {
            string configDirectory = GetConfigDirectory();
            EnsureConfigDirectory(configDirectory);
            string configPath = Path.Combine(configDirectory, "ClearNicknameConfig.ini");
            LoadBadWords(configPath);
            prefix = Localizer["ChatPrefix"];

            CheckAllPlayersNicknames();
        }

        private void LoadBadWords(string filePath)
        {
            if (File.Exists(filePath))
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        badWords.Add(trimmedLine);
                    }
                }
                Console.WriteLine($"{ModuleName} | Configuration file loaded. Badword's count: {badWords.Count}");
            }
            else
            {
                Console.WriteLine($"{ModuleName} | No configuration file found at {filePath}");
            }
        }

        [ConsoleCommand("css_clearnickname_reload", "Reload BadWords")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
        [RequiresPermissions("@css/root")]
        public void OnReloadBadWords(CCSPlayerController? player, CommandInfo commandInfo)
        {
            string configDirectory = GetConfigDirectory();
            string configPath = Path.Combine(configDirectory, "ClearNicknameConfig.ini");
            ReLoadBadWords(configPath);
        }

        private void ReLoadBadWords(string filePath)
        {
            badWords.Clear();
            if (File.Exists(filePath))
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        badWords.Add(trimmedLine);
                    }
                }
                Console.WriteLine($"{ModuleName} | Configuration file reloaded. Badword's count: {badWords.Count}");
            }
            else
            {
                Console.WriteLine($"{ModuleName} | No configuration file found at {filePath}");
            }
        }

        private static string GetConfigDirectory()
        {
            return Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/configs/plugins/ClearNickname/");
        }

        private void EnsureConfigDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Console.WriteLine($"{ModuleName} | Created configuration directory at: {directoryPath}");
            }
        }

        private void CheckAllPlayersNicknames()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player.IsValid && !player.IsBot && prefix != null)
                {
                    string? originalName = player.PlayerName;
                    (string cleanedName, int count) = CleanNickname(originalName);
                    if (originalName != cleanedName)
                    {
                        player.PlayerName = cleanedName;
                        player.PrintToChat(Localizer["OnConnect", prefix, count]);
                    }
                }
            }
        }

        [GameEventHandler]
        public HookResult PlayerConnect(EventPlayerConnectFull @event, GameEventInfo info)
        {
            if (@event?.Userid?.PlayerName == null || prefix == null)
                return HookResult.Continue;

            string originalName = @event.Userid.PlayerName;
            (string cleanedName, int count) = CleanNickname(originalName);

            if (originalName != cleanedName)
            {
                Logger.LogInformation($"{ModuleName} | Changed nickname from '{originalName}' to '{cleanedName}' for player with SteamID {@event.Userid.SteamID}");
                @event.Userid.PlayerName = cleanedName;
                @event.Userid.PrintToChat(Localizer["OnConnect", prefix, count]);
                return HookResult.Continue;
            }

            @event.Userid.PrintToChat(Localizer["NotFound", prefix]);

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnMapStart(EventGameNewmap @event, GameEventInfo info)
        {
            CheckAllPlayersNicknames();

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnGameInit(EventGameInit @event, GameEventInfo info)
        {
            CheckAllPlayersNicknames();

            return HookResult.Continue;
        }

        private (string, int) CleanNickname(string nickname)
        {
            int count = 0;
            foreach (string badWord in badWords)
            {
                var pattern = string.Join("\\W*", badWord.ToCharArray().Select(c => Regex.Escape(c.ToString())));
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                var matches = regex.Matches(nickname);
                count += matches.Count;
                nickname = regex.Replace(nickname, "");
            }
            return (nickname.Trim(), count);
        }
    }
}
