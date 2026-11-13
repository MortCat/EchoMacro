using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace EchoMacro.Service
{
    /// <summary>A global shortcut: modifier keys plus one key.</summary>
    public class HotKeySetting
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ModifierKeys Modifiers { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Key Key { get; set; }

        public HotKeySetting() { }
        public HotKeySetting(ModifierKeys modifiers, Key key)
        {
            Modifiers = modifiers;
            Key = key;
        }

        public bool SameAs(HotKeySetting other) => Modifiers == other.Modifiers && Key == other.Key;

        /// <summary>Display form, e.g. "Ctrl+Alt+R".</summary>
        public override string ToString()
        {
            var parts = new List<string>();
            if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");

            //Key.D1 is the "1" key.
            string name = Key.ToString();
            if (name.Length == 2 && name[0] == 'D' && char.IsDigit(name[1]))
                name = name.Substring(1);
            parts.Add(name);

            return string.Join("+", parts);
        }
    }

    /// <summary>
    /// Settings remembered between runs. Everything the app stores on its own lives in one folder
    /// next to the executable: the settings file, the recordings folder and the backups folder.
    /// </summary>
    public class AppSettings
    {
        private const ModifierKeys DefaultModifiers = ModifierKeys.Control | ModifierKeys.Alt;

        public int Delay { get; set; }
        public bool Repeat { get; set; }
        public double? WindowLeft { get; set; }
        public double? WindowTop { get; set; }

        public HotKeySetting RecordHotKey { get; set; } = new HotKeySetting(DefaultModifiers, Key.R);
        public HotKeySetting PlayHotKey { get; set; } = new HotKeySetting(DefaultModifiers, Key.P);
        public HotKeySetting CloseHotKey { get; set; } = new HotKeySetting(DefaultModifiers, Key.Q);

        /// <summary>Where Load and Save As start. Empty means the default folder inside <see cref="DataFolder"/>.</summary>
        public string? RecordingsFolder { get; set; }
        /// <summary>Where unsaved recordings are backed up. Empty means the default folder inside <see cref="DataFolder"/>.</summary>
        public string? BackupFolder { get; set; }

        /// <summary>Stop playback when the user keeps moving the real mouse.</summary>
        public bool AutoStopOnMouseMove { get; set; } = true;
        /// <summary>How long the movement has to last, so that a bumped mouse does not interrupt playback.</summary>
        public double AutoStopSeconds { get; set; } = 2;

        [JsonIgnore]
        public string EffectiveRecordingsFolder =>
            string.IsNullOrWhiteSpace(RecordingsFolder) ? Path.Combine(DataFolder, "Recordings") : RecordingsFolder;

        [JsonIgnore]
        public string EffectiveBackupFolder =>
            string.IsNullOrWhiteSpace(BackupFolder) ? Path.Combine(DataFolder, "Backups") : BackupFolder;

        /// <summary>The one folder for the app's own files: "EchoMacroData" next to the executable.</summary>
        public static string DataFolder { get; } = ResolveDataFolder();

        /// <summary>The settings in use. Change its properties and call <see cref="Save"/>.</summary>
        public static AppSettings Current { get; } = Load();

        private static string FilePath => Path.Combine(DataFolder, "settings.json");

        private static string ResolveDataFolder()
        {
            string besideExecutable = Path.Combine(AppContext.BaseDirectory, "EchoMacroData");
            try
            {
                Directory.CreateDirectory(besideExecutable);
                string probe = Path.Combine(besideExecutable, ".write-test");
                File.WriteAllText(probe, string.Empty);
                File.Delete(probe);
                return besideExecutable;
            }
            catch (Exception)
            {
                //The executable sits somewhere read-only (e.g. Program Files); use the per-user folder instead.
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EchoMacro");
            }
        }

        /// <summary>Never fails: a missing or damaged settings file just means default settings.</summary>
        private static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
            }
            catch (Exception)
            {
                //Fall through to defaults.
            }
            return new AppSettings();
        }

        /// <summary>Never fails: not being able to remember settings must not get in the way of closing.</summary>
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(DataFolder);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception)
            {
                //Settings are a convenience only.
            }
        }
    }
}
