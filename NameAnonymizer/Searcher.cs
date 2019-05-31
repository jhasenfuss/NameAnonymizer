using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NameAnonymizer
{
    internal class Searcher
    {
        public Searcher(string rootDir)
        {
            RootDir = rootDir;
            AnalyzedPlayers = new List<Player>();
        }

        private string RootDir { get; }
        private List<Player> AnalyzedPlayers { get; set; }
        public static string RegEx { get; set; } = "^(from|to)?[^:]*";

        public Task<List<Player>> AnalyzePlayers()
        {
            return Task.Run(() =>
            {
                AnalyzedPlayers.Clear();

                var rootInfo = new DirectoryInfo(RootDir);
                var files = rootInfo.GetFiles("*.txt", SearchOption.AllDirectories).OrderBy(d => d.CreationTime)
                    .ToList();

                foreach (var file in files)
                {
                    var filename = file.FullName.Replace(RootDir + "\\", "");
                    var lines = File.ReadAllLines(file.FullName, Encoding.GetEncoding("Windows-1252"));
                    var i = 1;
                    foreach (var line in lines)
                    {
                        var match = Regex.Match(line, RegEx,
                            RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        if (!match.Success) continue;

                        var player = AnalyzedPlayers.FirstOrDefault(d => d.Original == match.Value);

                        if (player == null)
                        {
                            player = new Player {Original = match.Value};
                            AnalyzedPlayers.Add(player);
                        }

                        if (!player.Files.Contains(filename)) player.Files.Add(filename);
                        player.Lines.Add(new TextLine
                        {
                            Text = line,
                            Filename = filename,
                            FullFilename = file.FullName,
                            Row = i
                        });

                        i++;
                    }
                }

                AnalyzedPlayers = AnalyzedPlayers.OrderBy(d => d.Original).ToList();
                var pi = 1;
                AnalyzedPlayers.ForEach(d => d.Replaced = "Player" + pi++.ToString("D4"));

                return AnalyzedPlayers;
            });
        }
    }
}