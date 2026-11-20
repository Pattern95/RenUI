using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace RenUI_Launcher
{
    public partial class MainWindow : Window
    {
        private const string CurrentVersion = "1.8.1";
        private const string SteamAppId = "2827200";
        private const string GitHubOwner = "Pattern95";
        private const string GitHubRepo = "RenUI";
        private const string RawVersionUrl = "https://raw.githubusercontent.com/Pattern95/RenUI/main/version.json";
        private const string ReleasesApiUrl = "https://api.github.com/repos/Pattern95/RenUI/releases/latest";

        private string steamMimesisDir = @"C:\Program Files (x86)\Steam\steamapps\common\MIMESIS";
        private string r2ProfileDir = @"C:\Users\Family\AppData\Roaming\r2modmanPlus-local\Mimesis\profiles\Default";
        private string r2CacheDir181 = @"C:\Users\Family\AppData\Roaming\r2modmanPlus-local\Mimesis\cache\Pattern-RenUI\1.8.1";
        private string r2CacheDir180 = @"C:\Users\Family\AppData\Roaming\r2modmanPlus-local\Mimesis\cache\Pattern-RenUI\1.8.0";
        private string sourceDllPath = @"C:\My_Games\Mimesis\InfiniteStamina\bin\Release\netstandard2.1\InfiniteStamina.dll";

        private float itemColorR = 1.0f;
        private float itemColorG = 0.85f;
        private float itemColorB = 0.20f;
        private bool isUpdating = false;

        public MainWindow()
        {
            InitializeComponent();
            CheckSystemStatus();
            LoadConfig();

            Loaded += async (s, e) =>
            {
                await Task.Delay(800);
                _ = CheckForUpdatesAsync(autoStartup: true);
            };
        }

        private void Header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig(silent: true);
            Close();
        }

        private void CheckSystemStatus()
        {
            bool steamFound = Directory.Exists(steamMimesisDir);
            TxtSteamStatus.Text = steamFound ? "Найден ✓" : "Не найден ✗";

            string melonDll = System.IO.Path.Combine(steamMimesisDir, "version.dll");
            bool melonFound = File.Exists(melonDll) || Directory.Exists(System.IO.Path.Combine(steamMimesisDir, "MelonLoader"));
            TxtMelonStatus.Text = melonFound ? "v0.7.3 Активен ✓" : "Не установлен ✗";

            string modDll = System.IO.Path.Combine(steamMimesisDir, "Mods", "InfiniteStamina.dll");
            bool modFound = File.Exists(modDll) || File.Exists(sourceDllPath);
            TxtModStatus.Text = modFound ? $"v{CurrentVersion} Установлен ✓" : "Не найден ✗";

            TxtPathsDisplay.Text = $"Steam путь: {steamMimesisDir}\n" +
                                  $"r2modman профиль: {r2ProfileDir}\n" +
                                  $"Файл мода: {modDll}\n" +
                                  $"Репозиторий: https://github.com/{GitHubOwner}/{GitHubRepo}\n" +
                                  $"Горячая клавиша в игре: [F7]";
        }

        private async Task CheckForUpdatesAsync(bool autoStartup)
        {
            if (isUpdating) return;
            isUpdating = true;

            try
            {
                TxtUpdateStatus.Text = "Проверка...";
                DotUpdate.Fill = Brushes.SkyBlue;

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "RenUI-Launcher");
                    client.Timeout = TimeSpan.FromSeconds(8);

                    string remoteVersion = null;
                    string downloadDllUrl = null;
                    string changelog = "";

                    // 1. Check raw version.json
                    try
                    {
                        var res = await client.GetAsync(RawVersionUrl);
                        if (res.IsSuccessStatusCode)
                        {
                            string json = await res.Content.ReadAsStringAsync();
                            using (var doc = JsonDocument.Parse(json))
                            {
                                if (doc.RootElement.TryGetProperty("version", out var vElem))
                                {
                                    remoteVersion = vElem.GetString();
                                }
                                if (doc.RootElement.TryGetProperty("mod_dll_url", out var mElem))
                                {
                                    downloadDllUrl = mElem.GetString();
                                }
                                if (doc.RootElement.TryGetProperty("changelog", out var clElem))
                                {
                                    changelog = clElem.GetString();
                                }
                            }
                        }
                    }
                    catch { }

                    // 2. If raw file not available, check GitHub Releases API
                    if (string.IsNullOrEmpty(remoteVersion))
                    {
                        try
                        {
                            var res = await client.GetAsync(ReleasesApiUrl);
                            if (res.IsSuccessStatusCode)
                            {
                                string json = await res.Content.ReadAsStringAsync();
                                using (var doc = JsonDocument.Parse(json))
                                {
                                    if (doc.RootElement.TryGetProperty("tag_name", out var tagElem))
                                    {
                                        remoteVersion = tagElem.GetString();
                                    }
                                    if (doc.RootElement.TryGetProperty("body", out var bElem))
                                    {
                                        changelog = bElem.GetString();
                                    }
                                    if (doc.RootElement.TryGetProperty("assets", out var assetsElem) && assetsElem.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var asset in assetsElem.EnumerateArray())
                                        {
                                            if (asset.TryGetProperty("name", out var n) &&
                                                (n.GetString().Equals("InfiniteStamina.dll", StringComparison.OrdinalIgnoreCase) ||
                                                 n.GetString().EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                downloadDllUrl = asset.GetProperty("browser_download_url").GetString();
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    // 3. Process version comparison
                    if (!string.IsNullOrEmpty(remoteVersion))
                    {
                        string cleanRemote = remoteVersion.TrimStart('v', 'V');
                        string cleanCurrent = CurrentVersion.TrimStart('v', 'V');

                        if (IsVersionNewer(cleanRemote, cleanCurrent))
                        {
                            TxtUpdateStatus.Text = $"v{cleanRemote} доступна!";
                            DotUpdate.Fill = Brushes.Gold;

                            if (!string.IsNullOrEmpty(downloadDllUrl))
                            {
                                TxtStatusMsg.Text = $"⚡ Найдено обновление v{cleanRemote}! Авто-загрузка...";
                                TxtStatusMsg.Foreground = Brushes.Gold;

                                byte[] dllBytes = await client.GetByteArrayAsync(downloadDllUrl);

                                // Deploy to Steam
                                string steamMods = System.IO.Path.Combine(steamMimesisDir, "Mods");
                                if (Directory.Exists(steamMods))
                                {
                                    File.WriteAllBytes(System.IO.Path.Combine(steamMods, "InfiniteStamina.dll"), dllBytes);
                                }

                                // Deploy to r2modman
                                string r2ModDir = System.IO.Path.Combine(r2ProfileDir, "Mods", "Pattern-RenUI");
                                if (Directory.Exists(r2ModDir))
                                {
                                    File.WriteAllBytes(System.IO.Path.Combine(r2ModDir, "InfiniteStamina.dll"), dllBytes);
                                }

                                // Deploy to local source
                                if (File.Exists(sourceDllPath))
                                {
                                    File.WriteAllBytes(sourceDllPath, dllBytes);
                                }

                                TxtUpdateStatus.Text = $"Обновлен до v{cleanRemote} ✓";
                                DotUpdate.Fill = Brushes.MediumSpringGreen;
                                TxtModStatus.Text = $"v{cleanRemote} Актуален ✓";

                                string info = !string.IsNullOrEmpty(changelog) ? $" ({changelog})" : "";
                                TxtStatusMsg.Text = $"✓ RenUI успешно обновлен до v{cleanRemote}{info}!";
                                TxtStatusMsg.Foreground = Brushes.MediumSpringGreen;
                                return;
                            }
                        }
                    }

                    // Up to date
                    TxtUpdateStatus.Text = "Актуально ✓";
                    DotUpdate.Fill = Brushes.MediumSpringGreen;
                    if (!autoStartup)
                    {
                        TxtStatusMsg.Text = $"✓ У вас установлена самая свежая версия RenUI (v{CurrentVersion}). Обновлений нет.";
                        TxtStatusMsg.Foreground = Brushes.MediumSpringGreen;
                    }
                }
            }
            catch (Exception ex)
            {
                TxtUpdateStatus.Text = "Офлайн";
                DotUpdate.Fill = Brushes.Gray;
                if (!autoStartup)
                {
                    TxtStatusMsg.Text = $"⚠ Не удалось проверить обновления: {ex.Message}";
                    TxtStatusMsg.Foreground = Brushes.Orange;
                }
            }
            finally
            {
                isUpdating = false;
            }
        }

        private bool IsVersionNewer(string remote, string current)
        {
            if (Version.TryParse(remote, out var vRem) && Version.TryParse(current, out var vCur))
            {
                return vRem > vCur;
            }
            return string.Compare(remote, current, StringComparison.OrdinalIgnoreCase) > 0;
        }

        private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            await CheckForUpdatesAsync(autoStartup: false);
        }

        private List<string> GetConfigPaths()
        {
            var list = new List<string>();
            string r2Cfg = System.IO.Path.Combine(r2ProfileDir, "UserData", "MelonPreferences.cfg");
            if (File.Exists(r2Cfg) || Directory.Exists(System.IO.Path.GetDirectoryName(r2Cfg))) list.Add(r2Cfg);

            string steamCfg = System.IO.Path.Combine(steamMimesisDir, "UserData", "MelonPreferences.cfg");
            if (File.Exists(steamCfg) || Directory.Exists(System.IO.Path.GetDirectoryName(steamCfg))) list.Add(steamCfg);

            return list;
        }

        private void LoadConfig()
        {
            try
            {
                var paths = GetConfigPaths();
                string targetPath = paths.Count > 0 ? paths[0] : null;

                if (targetPath != null && File.Exists(targetPath))
                {
                    var lines = File.ReadAllLines(targetPath);
                    bool inMenuSection = false;

                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                        {
                            inMenuSection = trimmed.Equals("[MimesisMenu]", StringComparison.OrdinalIgnoreCase);
                            continue;
                        }

                        if (!inMenuSection || !trimmed.Contains("=")) continue;

                        var parts = trimmed.Split(new[] { '=' }, 2);
                        string key = parts[0].Trim();
                        string val = parts[1].Trim().Trim('"');

                        switch (key)
                        {
                            case "GodMode":
                                ChkGodMode.IsChecked = bool.TryParse(val, out bool gm) && gm;
                                break;
                            case "InfiniteStamina":
                                ChkInfiniteStamina.IsChecked = bool.TryParse(val, out bool isStam) && isStam;
                                break;
                            case "NoWeightSlowdown":
                                ChkNoWeight.IsChecked = bool.TryParse(val, out bool nw) && nw;
                                break;
                            case "SilentSteps":
                                ChkSilentSteps.IsChecked = bool.TryParse(val, out bool ss) && ss;
                                break;
                            case "InfiniteBattery":
                                ChkInfiniteBattery.IsChecked = bool.TryParse(val, out bool ib) && ib;
                                break;
                            case "AntiContamination":
                                ChkAntiConta.IsChecked = bool.TryParse(val, out bool ac) && ac;
                                break;
                            case "ShowTeamStatus":
                                ChkTeamStatus.IsChecked = bool.TryParse(val, out bool sts) && sts;
                                break;
                            case "FullBrightEnabled":
                                ChkFullBright.IsChecked = bool.TryParse(val, out bool fb) && fb;
                                break;
                            case "FullBrightIntensity":
                                if (float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out float fbi))
                                {
                                    SldFullBright.Value = fbi;
                                    TxtFullBrightVal.Text = $"{fbi:0.0}x";
                                }
                                break;
                            case "ItemEspEnabled":
                                ChkItemEsp.IsChecked = bool.TryParse(val, out bool ie) && ie;
                                break;
                            case "ItemEspDistance":
                                if (float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out float ied))
                                {
                                    SldItemDist.Value = ied;
                                    TxtItemDistVal.Text = $"{(int)ied} м";
                                }
                                break;
                            case "ItemEspColorR":
                                float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out itemColorR);
                                break;
                            case "ItemEspColorG":
                                float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out itemColorG);
                                break;
                            case "ItemEspColorB":
                                float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out itemColorB);
                                break;
                            case "ActorEspEnabled":
                                ChkActorEsp.IsChecked = bool.TryParse(val, out bool ae) && ae;
                                break;
                            case "ActorEspDistance":
                                if (float.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out float aed))
                                {
                                    SldActorDist.Value = aed;
                                    TxtActorDistVal.Text = $"{(int)aed} м";
                                }
                                break;
                            case "ActorEspShowAllies":
                                ChkEspAllies.IsChecked = bool.TryParse(val, out bool aesa) && aesa;
                                break;
                            case "ActorEspShowEnemies":
                                ChkEspEnemies.IsChecked = bool.TryParse(val, out bool aese) && aese;
                                break;
                            case "ActorEspShowName":
                                ChkEspName.IsChecked = bool.TryParse(val, out bool aesn) && aesn;
                                break;
                            case "ActorEspShowDist":
                                ChkEspDist.IsChecked = bool.TryParse(val, out bool aesd) && aesd;
                                break;
                            case "ActorEspShowHp":
                                ChkEspHp.IsChecked = bool.TryParse(val, out bool aesh) && aesh;
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TxtStatusMsg.Text = $"⚠ Загрузка настроек: {ex.Message}";
            }
        }

        private void SaveConfig(bool silent = false)
        {
            try
            {
                var dict = new Dictionary<string, string>
                {
                    ["GodMode"] = (ChkGodMode.IsChecked == true).ToString().ToLower(),
                    ["InfiniteStamina"] = (ChkInfiniteStamina.IsChecked == true).ToString().ToLower(),
                    ["NoWeightSlowdown"] = (ChkNoWeight.IsChecked == true).ToString().ToLower(),
                    ["SilentSteps"] = (ChkSilentSteps.IsChecked == true).ToString().ToLower(),
                    ["InfiniteBattery"] = (ChkInfiniteBattery.IsChecked == true).ToString().ToLower(),
                    ["AntiContamination"] = (ChkAntiConta.IsChecked == true).ToString().ToLower(),
                    ["ShowTeamStatus"] = (ChkTeamStatus.IsChecked == true).ToString().ToLower(),
                    ["FullBrightEnabled"] = (ChkFullBright.IsChecked == true).ToString().ToLower(),
                    ["FullBrightIntensity"] = SldFullBright.Value.ToString("0.0", CultureInfo.InvariantCulture),
                    ["ItemEspEnabled"] = (ChkItemEsp.IsChecked == true).ToString().ToLower(),
                    ["ItemEspDistance"] = SldItemDist.Value.ToString("0.0", CultureInfo.InvariantCulture),
                    ["ItemEspColorR"] = itemColorR.ToString("0.00", CultureInfo.InvariantCulture),
                    ["ItemEspColorG"] = itemColorG.ToString("0.00", CultureInfo.InvariantCulture),
                    ["ItemEspColorB"] = itemColorB.ToString("0.00", CultureInfo.InvariantCulture),
                    ["ActorEspEnabled"] = (ChkActorEsp.IsChecked == true).ToString().ToLower(),
                    ["ActorEspDistance"] = SldActorDist.Value.ToString("0.0", CultureInfo.InvariantCulture),
                    ["ActorEspShowAllies"] = (ChkEspAllies.IsChecked == true).ToString().ToLower(),
                    ["ActorEspShowEnemies"] = (ChkEspEnemies.IsChecked == true).ToString().ToLower(),
                    ["ActorEspShowName"] = (ChkEspName.IsChecked == true).ToString().ToLower(),
                    ["ActorEspShowDist"] = (ChkEspDist.IsChecked == true).ToString().ToLower(),
                    ["ActorEspShowHp"] = (ChkEspHp.IsChecked == true).ToString().ToLower(),
                };

                var paths = GetConfigPaths();
                foreach (var path in paths)
                {
                    UpdateConfigFile(path, dict);
                }

                if (!silent)
                {
                    TxtStatusMsg.Text = $"✓ Настройки сохранены в {paths.Count} конфигурационных файлов ({DateTime.Now:HH:mm:ss})";
                    TxtStatusMsg.Foreground = Brushes.MediumSpringGreen;
                }
            }
            catch (Exception ex)
            {
                TxtStatusMsg.Text = $"❌ Ошибка сохранения: {ex.Message}";
                TxtStatusMsg.Foreground = Brushes.Crimson;
            }
        }

        private void UpdateConfigFile(string filePath, Dictionary<string, string> values)
        {
            string dir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var lines = File.Exists(filePath) ? new List<string>(File.ReadAllLines(filePath)) : new List<string>();
            var updatedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int sectionStartIndex = -1;
            int sectionEndIndex = lines.Count;

            for (int i = 0; i < lines.Count; i++)
            {
                string tr = lines[i].Trim();
                if (tr.Equals("[MimesisMenu]", StringComparison.OrdinalIgnoreCase))
                {
                    sectionStartIndex = i;
                    for (int j = i + 1; j < lines.Count; j++)
                    {
                        if (lines[j].Trim().StartsWith("[") && lines[j].Trim().EndsWith("]"))
                        {
                            sectionEndIndex = j;
                            break;
                        }
                    }
                    break;
                }
            }

            if (sectionStartIndex == -1)
            {
                lines.Add("");
                lines.Add("[MimesisMenu]");
                foreach (var kvp in values)
                {
                    lines.Add($"  {kvp.Key} = {kvp.Value}");
                }
            }
            else
            {
                for (int i = sectionStartIndex + 1; i < sectionEndIndex; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Contains("="))
                    {
                        var key = line.Split('=')[0].Trim();
                        if (values.TryGetValue(key, out string newVal))
                        {
                            lines[i] = $"  {key} = {newVal}";
                            updatedKeys.Add(key);
                        }
                    }
                }

                // Add any missing keys
                int insertPos = sectionEndIndex;
                foreach (var kvp in values)
                {
                    if (!updatedKeys.Contains(kvp.Key))
                    {
                        lines.Insert(insertPos, $"  {kvp.Key} = {kvp.Value}");
                        insertPos++;
                        sectionEndIndex++;
                    }
                }
            }

            File.WriteAllLines(filePath, lines);
        }

        private void SyncModFiles()
        {
            try
            {
                if (File.Exists(sourceDllPath))
                {
                    // 1. Steam Mods folder
                    string steamMods = System.IO.Path.Combine(steamMimesisDir, "Mods");
                    if (Directory.Exists(steamMods))
                    {
                        File.Copy(sourceDllPath, System.IO.Path.Combine(steamMods, "InfiniteStamina.dll"), true);
                    }

                    // 2. r2modman Profile
                    string r2ModDir = System.IO.Path.Combine(r2ProfileDir, "Mods", "Pattern-RenUI");
                    if (Directory.Exists(r2ModDir))
                    {
                        File.Copy(sourceDllPath, System.IO.Path.Combine(r2ModDir, "InfiniteStamina.dll"), true);
                    }

                    // 3. r2modman Cache
                    if (Directory.Exists(r2CacheDir181))
                    {
                        File.Copy(sourceDllPath, System.IO.Path.Combine(r2CacheDir181, "InfiniteStamina.dll"), true);
                    }
                    if (Directory.Exists(r2CacheDir180))
                    {
                        File.Copy(sourceDllPath, System.IO.Path.Combine(r2CacheDir180, "InfiniteStamina.dll"), true);
                    }

                    TxtStatusMsg.Text = "✓ RenUI v1.8.1 успешно синхронизирован со Steam и r2modman!";
                    TxtStatusMsg.Foreground = Brushes.MediumSpringGreen;
                }
            }
            catch (Exception ex)
            {
                TxtStatusMsg.Text = $"⚠ Ошибка синхронизации: {ex.Message}";
            }
        }

        private void BtnLaunchGame_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SyncModFiles();
                SaveConfig(silent: true);

                TxtStatusMsg.Text = "🚀 Запуск MIMESIS с RenUI... Меню в игре на [F7]!";
                TxtStatusMsg.Foreground = Brushes.DeepSkyBlue;

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = $"steam://run/{SteamAppId}",
                        UseShellExecute = true
                    });
                }
                catch
                {
                    string exePath = System.IO.Path.Combine(steamMimesisDir, "MIMESIS.exe");
                    if (File.Exists(exePath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = exePath,
                            WorkingDirectory = steamMimesisDir,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        throw new FileNotFoundException($"MIMESIS.exe не найден в {steamMimesisDir}");
                    }
                }
            }
            catch (Exception ex)
            {
                TxtStatusMsg.Text = $"❌ Ошибка запуска: {ex.Message}";
                TxtStatusMsg.Foreground = Brushes.Crimson;
            }
        }

        private void BtnSyncMod_Click(object sender, RoutedEventArgs e)
        {
            SyncModFiles();
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(steamMimesisDir))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{steamMimesisDir}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        }

        private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
        }

        private void SldFullBright_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtFullBrightVal != null)
            {
                TxtFullBrightVal.Text = $"{e.NewValue:0.0}x";
            }
        }

        private void SldItemDist_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtItemDistVal != null)
            {
                TxtItemDistVal.Text = $"{(int)e.NewValue} м";
            }
        }

        private void SldActorDist_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtActorDistVal != null)
            {
                TxtActorDistVal.Text = $"{(int)e.NewValue} м";
            }
        }

        private void BtnColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                var parts = tag.Split(';');
                if (parts.Length == 3 &&
                    float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float r) &&
                    float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float g) &&
                    float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float b))
                {
                    itemColorR = r;
                    itemColorG = g;
                    itemColorB = b;
                    TxtStatusMsg.Text = $"✓ Выбран цвет подсветки: {btn.Content}";
                    TxtStatusMsg.Foreground = Brushes.MediumSpringGreen;
                }
            }
        }
    }
}
