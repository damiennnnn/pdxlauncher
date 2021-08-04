
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Linq;
using System.Windows.Shapes;
using MahApps.Metro;
using System.IO;
using MaterialDesignThemes.Wpf;
using Path = System.IO.Path;
using System.Diagnostics;
using System.Web.Script.Serialization;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System.IO.Compression;

namespace pdxlauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {

        public string mod_directory = string.Empty;
        public string dlc_load = string.Empty;
        public string eu_folder = string.Empty;
        public string hoi_folder = string.Empty;

        public string game_executable = string.Empty;
        public string game_folder = string.Empty;
        public struct Mod
        {
            public string name;
            public string version;
            public string supported_ver;
            public string picture;
            public string file_name;
        }
        public EnabledModList enabled = new EnabledModList();
        public class EnabledModList
        {
            public List<string> disabled_dlcs { get; set; }
            public List<string> enabled_mods { get; set; }
        }

        public List<CheckBox> search = new List<CheckBox>();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;

            string title = "";
            string game_folder = "";
#if EU4
            title = "EU4LAUNCH";
            game_folder = "Europa Universalis IV";
#elif HOI
            title = "HOI4LAUNCH"; 
            game_folder = "Hearts of Iron IV";
#endif

            if (!File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), title)))
            {
                first_launch();
            }
            else
            {
                string[] tmp = File.ReadAllLines(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), title));
                game_folder = tmp[0]; game_executable = tmp[1];
            }
            eu_folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Paradox Interactive", game_folder);
            dlc_load = Path.Combine(eu_folder, "dlc_load.json");
            mod_directory = Path.Combine(eu_folder, "mod");
            enabled = new JavaScriptSerializer().Deserialize<EnabledModList>(File.ReadAllText(dlc_load));
            creamapi_btn.IsEnabled = creamapi_status();
#if EU4
            extractbtn.Visibility = creamapi_status() ? Visibility.Hidden : Visibility.Visible;
#else
            extractbtn.Visibility = Visibility.Hidden;
#endif
            parse_mods();
        }

        private bool creamapi_status()
        {
#if EU4
            return (Directory.Exists(Path.Combine(game_folder, "pdxlaunch")) && File.Exists(Path.Combine(game_folder, "cream_api.ini")) && File.Exists(Path.Combine(game_folder, "pdxlaunch", "creamapi.dll")) && File.Exists(Path.Combine(game_folder, "pdxlaunch", "original.dll")));
#else
            return false;
#endif
        }

        private void extract(object sender, RoutedEventArgs e)
        {
            using (ZipArchive archive = new ZipArchive(new MemoryStream(Properties.Resources.cream_api)))
            {
                if (!Directory.Exists(Path.Combine(game_folder, "pdxlaunch")))
                    Directory.CreateDirectory(Path.Combine(game_folder, "pdxlaunch"));
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var stream = entry.Open())
                        {
                            using (FileStream fs = new FileStream(Path.Combine(game_folder, "pdxlaunch", entry.FullName), FileMode.Create, FileAccess.Write))
                            {
                                stream.CopyTo(fs);
                            }
                        }
                    }
                    else
                    {
                        using (var stream = entry.Open())
                        {
                            using (FileStream fs = new FileStream(Path.Combine(game_folder, entry.FullName), FileMode.Create, FileAccess.Write))
                            {
                                stream.CopyTo(fs);
                            }
                        }
                    }
                }
            }
            (sender as Button).Visibility = Visibility.Hidden;
            (sender as Button).IsEnabled = false;
            creamapi_btn.IsEnabled = true;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Button close = this.FindChild<Button>("PART_Close");
            close.Click += close_Click;
        }
        void close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        public async void first_launch()
        {
            string game_exe = "";
            string title = "";
#if EU4
            game_exe = "eu4.exe";
            title = "EU4LAUNCH";
#elif HOI
            game_exe = "hoi4.exe";
            title = "HOI4LAUNCH";
#endif
            MessageDialogResult result = await this.ShowMessageAsync("First Launch", "Locate the game folder in SteamApps", MessageDialogStyle.Affirmative);
            if (result == MessageDialogResult.Affirmative)
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    bool valid = false;
                    while (!valid)
                    {
                        System.Windows.Forms.DialogResult dialogResult = dialog.ShowDialog();
                        if (dialogResult == System.Windows.Forms.DialogResult.OK)
                        {
                            MessageBox.Show(dialog.SelectedPath);
                            var test = Directory.EnumerateFiles(dialog.SelectedPath + @"\");
                            if (test.FirstOrDefault(s => s.Contains(game_exe)) != null)
                            {
                                game_folder = dialog.SelectedPath + @"\";
                                game_executable = test.First(s => s.Contains(game_exe));
                                using (var write = File.CreateText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), title)))
                                {
                                    write.WriteLine(game_folder);
                                    write.WriteLine(game_executable);
                                }
                                valid = true;
                            }
                            else
                            {
                                await this.ShowMessageAsync("Error", "Invalid game directory", MessageDialogStyle.Affirmative);
                            }
                        }
                        else
                        {
                            await this.ShowMessageAsync("Error", "Invalid game directory", MessageDialogStyle.Affirmative);
                        }
                    }
                }
            }
        }

        public void parse_mods()
        {
            mod_list.Items.Clear();
            string[] paths = Directory.GetFiles(mod_directory,"*.mod");

            var palette = new PaletteHelper().QueryPalette();
            var hue = palette.PrimarySwatch.PrimaryHues.ToArray()[palette.PrimaryLightHueIndex];
            var alt_hue = palette.PrimarySwatch.PrimaryHues.ToArray()[palette.PrimaryDarkHueIndex];
            SolidColorBrush brush = new SolidColorBrush(hue.Color);
            SolidColorBrush alt_brush = new SolidColorBrush(alt_hue.Color);
            foreach (string mod in paths)
            {
                string file_name = Path.GetFileName(mod);
                string[] lines = File.ReadAllLines(mod);
                

                CheckBox box = new CheckBox();
                box.Width = 772;
                box.Foreground = brush;
                box.Click += Box_Click;

                Mod tmp = new Mod();

                tmp.file_name = file_name;

                box.ToolTip = new Label() { Content = string.Concat(file_name, Environment.NewLine), Foreground = alt_brush };
                // name //

                string name_str = lines.First(s => s.Contains("name=\""));
                string[] name_arr = name_str.Split('=');
                name_str = name_arr[1].Replace("\"", "");


                string ver_str = lines.FirstOrDefault(s => s.Contains("supported_version=\""));
                if (!string.IsNullOrEmpty(ver_str))
                {
                    string[] ver_arr = ver_str.Split('=');
                    ver_str = ver_arr[1].Replace("\"", "");
                    tmp.supported_ver = ver_str;
                    ((Label)box.ToolTip).Content += string.Concat("Supported Version: ", ver_str);
                }



                tmp.name = name_str;
                box.Content = name_str;
                box.DataContext = tmp;

                if (enabled.enabled_mods.Contains(string.Concat("mod/", file_name)))
                {
                    box.IsChecked = true;
                }

                mod_list.Items.Add(box);
                search.Add(box);
            }
            var boxes = mod_list.Items.Cast<CheckBox>().ToList();
            boxes = boxes.FindAll(x => x.IsChecked == true);
            mod_count.Text = string.Format("{0} mods enabled", boxes.Count);
        }

        private void Box_Click(object sender, RoutedEventArgs e)
        {
            var boxes = mod_list.Items.Cast<CheckBox>().ToList();
            boxes = boxes.FindAll(x => x.IsChecked == true);
            mod_count.Text = string.Format("{0} mods enabled", boxes.Count);
            Mod mod = (Mod)((sender as CheckBox).DataContext);
            string path = string.Concat("mod/", mod.file_name);
            if (!enabled.enabled_mods.Contains(path))
                enabled.enabled_mods.Add(path);
            else
                enabled.enabled_mods.Remove(path);
            var serialised = new JavaScriptSerializer().Serialize(enabled);
            File.WriteAllText(dlc_load, serialised.ToString());
        }

        // width 772
        private void mod_dir_click(object sender, RoutedEventArgs e)
        {
            Process.Start(mod_directory);
        }

        private void refresh(object sender, RoutedEventArgs e)
        {
            parse_mods();
        }

        private void game_dir_click(object sender, RoutedEventArgs e)
        {
            Process.Start(game_folder);
        }

        private void launch_eu4(object sender, RoutedEventArgs e)
        {
            if (creamapi_status())
                File.Copy(Path.Combine(game_folder, "pdxlaunch", "original.dll"), Path.Combine(game_folder, "steam_api64.dll"), true);
            Process.Start(game_executable);
        }

        private void creamapi(object sender, RoutedEventArgs e)
        {
            File.Copy(Path.Combine(game_folder, "pdxlaunch", "creamapi.dll"), Path.Combine(game_folder, "steam_api64.dll"), true);
            File.Copy(Path.Combine(game_folder, "pdxlaunch", "original.dll"), Path.Combine(game_folder, "steam_api64_o.dll"), true);
            Process.Start(game_executable);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            mod_list.Items.Clear();
            foreach (var ee in search.FindAll(x => x.Content.ToString().ToLower().Contains(((TextBox)sender).Text.ToLower())))
            {
                mod_list.Items.Add(ee);
            }
        }
    }
}
