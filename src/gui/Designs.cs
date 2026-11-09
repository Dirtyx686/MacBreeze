// MacBreeze 2.1 interface layouts. GPL-2.0-only; see LICENSE and NOTICE.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MacBreeze {
    public partial class MainWindow {
        bool DesignNavTop { get { return Look.Key == "studio" ? false : (Look.Key == "cockpit" || Look.Key == "focus" ? true : Settings.NavTop); } }
        bool NewDesign { get { return Look.Key == "studio" || Look.Key == "cockpit" || Look.Key == "focus"; } }
        void ConfigureDesignLayout(Grid grid, Panel fansPanel, Panel temperatures) {
            if (!NewDesign) return;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.SizeChanged += delegate {
                bool stacked = grid.ActualWidth < 950;
                grid.ColumnDefinitions[1].MinWidth = 0;
                grid.ColumnDefinitions[1].Width = stacked ? new GridLength(0) : new GridLength(272);
                Grid.SetColumnSpan(fansPanel, stacked ? 2 : 1);
                Grid.SetColumn(temperatures, stacked ? 0 : 1);
                Grid.SetRow(temperatures, stacked ? 1 : 0);
                Grid.SetColumnSpan(temperatures, stacked ? 2 : 1);
            };
        }
        void ApplyAccent() {
            string value = Settings.Accent;
            if (!string.IsNullOrEmpty(value) && System.Text.RegularExpressions.Regex.IsMatch(value, "^#[0-9a-fA-F]{6}$")) SetC(ACCENT, value);
            double luminance = 0;
            byte[] components = { ACCENT.Color.R, ACCENT.Color.G, ACCENT.Color.B };
            double[] weights = { .2126, .7152, .0722 };
            for (int i=0;i<3;i++) { double c=components[i]/255.0; luminance += weights[i]*(c<=.04045 ? c/12.92 : Math.Pow((c+.055)/1.055,2.4)); }
            SetC(ACCENT_TEXT, luminance > .179 ? "#000000" : "#FFFFFF");
        }
        void AddDesignHeading(Panel page) {
            if (!NewDesign) return;
            string name = Look.Key == "studio" ? "Studio" : Look.Key == "cockpit" ? "Cockpit" : "Fokus";
            var heading = new StackPanel { Margin = new Thickness(20, 12, 20, 16) };
            heading.Children.Add(new TextBlock { Text = "MACBREEZE / " + name.ToUpperInvariant(), Foreground = SUB, FontSize = 11 });
            heading.Children.Add(new TextBlock { Text = I18n.T("Fans"), Foreground = TXT, FontSize = 28, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 5, 0, 0) });
            page.Children.Insert(0, heading);
        }
        Panel CreateDesignFanHost(Panel parent) {
            if (Look.Key != "studio") return parent;
            var host = new WrapPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Stretch };
            parent.Children.Add(host);
            host.SizeChanged += delegate {
                double width = Math.Max(0, parent.ActualWidth);
                int columns = width >= 1160 ? 2 : 1;
                foreach (FrameworkElement card in host.Children) card.Width = Math.Max(300, width / columns - 16);
            };
            return host;
        }
        void AddDesignFan(Panel host, StackPanel content, Grid header, FrameworkElement rpm, FanUi fan) {
            if (Look.Key == "cockpit") {
                fan.Rpm.FontFamily = new FontFamily("Consolas");
                fan.BarFill.CornerRadius = new CornerRadius(0);
                content.Children.Remove(header);
                var shell = new Grid();
                shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
                shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                header.Children.Remove(rpm);
                var instrument = new StackPanel { Margin = new Thickness(0, 12, 24, 12) };
                instrument.Children.Add(header); instrument.Children.Add(rpm);
                shell.Children.Add(instrument);
                Grid.SetColumn(content, 1); shell.Children.Add(content);
                // Stack the instrument above its controls on small windows.
                shell.SizeChanged += delegate {
                    bool narrow = shell.ActualWidth < 780;
                    shell.ColumnDefinitions[0].Width = narrow ? new GridLength(0) : new GridLength(210);
                    instrument.Visibility = narrow ? Visibility.Collapsed : Visibility.Visible;
                    if (narrow && !content.Children.Contains(header)) { instrument.Children.Remove(header); instrument.Children.Remove(rpm); header.Children.Add(rpm); Grid.SetColumn(rpm,1); content.Children.Insert(0,header); }
                    if (!narrow && content.Children.Contains(header)) { content.Children.Remove(header); header.Children.Remove(rpm); instrument.Children.Add(header); instrument.Children.Add(rpm); }
                };
                host.Children.Add(Card(shell)); return;
            }
            if (Look.Key == "focus") {
                content.Children.Remove(header);
                header.Children.Remove(rpm);
                header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Grid.SetColumn(rpm,0); Grid.SetColumnSpan(rpm,2); Grid.SetRow(rpm,1);
                rpm.HorizontalAlignment = HorizontalAlignment.Center; rpm.Margin = new Thickness(0,12,0,8);
                header.Children.Add(rpm);
                var compact = new Grid();
                compact.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
                compact.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1,GridUnitType.Star) });
                header.Margin = new Thickness(0,0,24,0); header.VerticalAlignment = VerticalAlignment.Center;
                compact.Children.Add(header); Grid.SetColumn(content,1); compact.Children.Add(content);
                host.Children.Add(Card(compact)); return;
            }
            var border = Card(content);
            if (Look.Key == "studio") border.MinWidth = 540;
            host.Children.Add(border);
        }
        void BuildAccentPicker(Panel parent) {
            bool de = Settings.Language == "de";
            parent.Children.Add(new TextBlock { Text = de ? "Akzentfarbe" : "Accent color", Foreground = TXT, FontSize = 13, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,16,0,8) });
            var row = new WrapPanel();
            foreach (string hex in new[]{ "#11776F", "#1765B5", "#6257AA", "#A34A89", "#A65D20", "#A83244" }) {
                string selected = hex;
                var button = new Button { Width = 38, Height = 34, Margin = new Thickness(0,0,8,8), Background = Hex(hex), BorderThickness = new Thickness(0), ToolTip = hex, Content = Settings.Accent == hex ? "✓" : "", Foreground = Brushes.White };
                System.Windows.Automation.AutomationProperties.SetName(button, (de ? "Akzentfarbe " : "Accent color ") + hex);
                button.Click += delegate { Settings.Accent = selected; Settings.Save(); ApplyTheme(Settings.Theme); };
                row.Children.Add(button);
            }
            var custom = new TextBox { Text = Settings.Accent, Width = 100, Margin = new Thickness(0,0,8,8), ToolTip = "#RRGGBB" };
            System.Windows.Automation.AutomationProperties.SetName(custom, de ? "Eigene Farbe als Hexcode" : "Custom color hex code");
            row.Children.Add(custom);
            var apply = new Button { Content = de ? "Übernehmen" : "Apply", Padding = new Thickness(12,6,12,6), Margin = new Thickness(0,0,8,8) };
            apply.Click += delegate {
                if (!System.Text.RegularExpressions.Regex.IsMatch(custom.Text, "^#[0-9a-fA-F]{6}$")) { MessageBox.Show(de ? "Bitte eine Farbe als #RRGGBB eingeben." : "Enter a color in #RRGGBB format."); return; }
                Settings.Accent = custom.Text.ToUpperInvariant(); Settings.Save(); ApplyTheme(Settings.Theme);
            };
            row.Children.Add(apply);
            var reset = new Button { Content = de ? "Designfarbe" : "Design default", Padding = new Thickness(12,6,12,6), Margin = new Thickness(0,0,0,8) };
            reset.Click += delegate { Settings.Accent = ""; custom.Text = ""; Settings.Save(); ApplyTheme(Settings.Theme); };
            row.Children.Add(reset); parent.Children.Add(row);
        }
    }
}
