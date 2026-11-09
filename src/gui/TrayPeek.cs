// MacBreeze - Kurzinfo beim Drüberfahren über das Symbol im Infobereich
// Teil von MacBreeze (basiert auf RPMac von golirino) - GPL-2.0-only. Siehe LICENSE und NOTICE.
//
// Fährt die Maus über das MacBreeze-Symbol neben der Uhr, erscheint ein kleines Fenster
// mit Lüfterdrehzahlen, den heißesten Sensoren und der CPU-Last – ohne das Hauptfenster
// zu öffnen. Es stiehlt keinen Fokus und verschwindet, sobald die Maus das Symbol verlässt.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MacBreeze {

    public partial class MainWindow {

        Window peek;
        StackPanel peekBody;
        System.Windows.Threading.DispatcherTimer peekTimer;
        System.Drawing.Point peekAnchor;          // Mausposition (Pixel) beim letzten Drüberfahren
        DateTime peekShownAt;

        // Letzte Messwerte aus der Aktualisierungsschleife (UI-Thread)
        internal Dictionary<string, double> PeekTemps = new Dictionary<string, double>();
        internal List<double[]> PeekFans = new List<double[]>();   // { index, rpm, max }
        internal double PeekCpu = double.NaN;

        internal void HookTrayPeek() {
            if (tray == null) return;
            tray.Text = "";   // kein zusätzlicher Windows-Tooltip über dem eigenen Fenster
            tray.MouseMove += delegate {
                peekAnchor = System.Windows.Forms.Cursor.Position;
                if (peek == null || !peek.IsVisible) ShowPeek();
            };
            tray.MouseDown += delegate { HidePeek(); };   // Klick/Menü hat Vorrang
            peekTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            peekTimer.Tick += delegate {
                if (peek == null || !peek.IsVisible) { peekTimer.Stop(); return; }
                var p = System.Windows.Forms.Cursor.Position;
                // Das Symbol ist ca. 24 px groß: weiter weg = Maus hat es verlassen
                if (Math.Abs(p.X - peekAnchor.X) > 16 || Math.Abs(p.Y - peekAnchor.Y) > 16) HidePeek();
                else if ((DateTime.Now - peekShownAt).TotalSeconds > 0.9) FillPeek();   // live mitlaufen
            };
        }

        void ShowPeek() {
            try {
                if (peek == null) BuildPeek();
                FillPeek();
                peek.Show();
                peek.UpdateLayout();
                PlacePeek();
                peekShownAt = DateTime.Now;
                peekTimer.Start();
            } catch (Exception ex) { App.LogError("TrayPeek", ex); }
        }

        void HidePeek() {
            try { if (peek != null) peek.Hide(); if (peekTimer != null) peekTimer.Stop(); } catch { }
        }

        void BuildPeek() {
            peekBody = new StackPanel { Width = 250 };
            var card = new Border {
                Background = CARD, BorderBrush = BORDER, BorderThickness = new Thickness(1),
                CornerRadius = Look.CR(10), Padding = new Thickness(14, 11, 14, 12), Margin = new Thickness(12),
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 16, ShadowDepth = 2, Opacity = 0.35 },
                Child = peekBody
            };
            peek = new Window {
                WindowStyle = WindowStyle.None, AllowsTransparency = true, Background = Brushes.Transparent,
                ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false, Topmost = true, ShowActivated = false,
                SizeToContent = SizeToContent.WidthAndHeight, Focusable = false, Content = card,
                FontFamily = new FontFamily(Look.Font)
            };
            // Maus auf dem Fenster selbst: schließen (es soll nur eine Vorschau sein)
            peek.MouseEnter += delegate { HidePeek(); };
        }

        void PlacePeek() {
            // Pixel -> WPF-Einheiten (DPI) und über dem Infobereich ausrichten
            var src = PresentationSource.FromVisual(peek);
            double sx = 1, sy = 1;
            if (src != null && src.CompositionTarget != null) { sx = src.CompositionTarget.TransformFromDevice.M11; sy = src.CompositionTarget.TransformFromDevice.M22; }
            double mx = peekAnchor.X * sx, my = peekAnchor.Y * sy;
            var wa = SystemParameters.WorkArea;
            double w = peek.ActualWidth, h = peek.ActualHeight;
            double left = mx - w / 2, top;
            if (my >= wa.Bottom - 2) top = wa.Bottom - h + 6;           // Taskleiste unten (Normalfall)
            else if (my <= wa.Top + 2) top = wa.Top - 6;                 // Taskleiste oben
            else top = my - h - 8;                                       // Überlaufbereich (^) o. ä.
            left = Math.Max(wa.Left - 6, Math.Min(left, wa.Right - w + 6));
            peek.Left = left; peek.Top = top;
        }

        void FillPeek() {
            if (peekBody == null) return;
            peekBody.Children.Clear();

            // Kopf: Name + Status
            var head = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            var name = new TextBlock { Text = "MacBreeze", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = TXT };
            var dot = new Border { Width = 7, Height = 7, CornerRadius = new CornerRadius(3.5), Margin = new Thickness(0, 0, 7, 0), VerticalAlignment = VerticalAlignment.Center,
                Background = (Smc.WritesAllowed && !Smc.SmcUnresponsive) ? GOOD : WARN };
            var left = new StackPanel { Orientation = Orientation.Horizontal };
            left.Children.Add(dot); left.Children.Add(name);
            head.Children.Add(left);
            if (!double.IsNaN(PeekCpu)) {
                var cpu = new TextBlock { Text = "CPU " + PeekCpu.ToString("0") + " %", FontSize = 11.5, Foreground = SUB, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
                DockPanel.SetDock(cpu, Dock.Right); head.Children.Insert(0, cpu);
            }
            peekBody.Children.Add(head);

            // Lüfter mit kleinem Balken
            foreach (var f in PeekFans) {
                double rpm = f[1], max = f[2];
                var g = new Grid { Margin = new Thickness(0, 2, 0, 2) };
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });
                g.Children.Add(new TextBlock { Text = I18n.T("Fan ") + (int)f[0], FontSize = 12, Foreground = SUB, VerticalAlignment = VerticalAlignment.Center });
                double frac = (max > 0 && !double.IsNaN(rpm)) ? Math.Max(0, Math.Min(1, rpm / max)) : 0;
                var track = new Grid { Height = 5, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
                track.Children.Add(new Border { Background = CHIP, CornerRadius = new CornerRadius(2.5) });
                var fill = new Border { Background = frac > 0.9 ? (Brush)RED : Look.Fill(ACCENT), CornerRadius = new CornerRadius(2.5), HorizontalAlignment = HorizontalAlignment.Left };
                track.Children.Add(fill);
                track.SizeChanged += delegate { fill.Width = track.ActualWidth * frac; };
                Grid.SetColumn(track, 1); g.Children.Add(track);
                var val = new TextBlock { Text = double.IsNaN(rpm) ? "—" : ((int)rpm) + " RPM", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = TXT, HorizontalAlignment = HorizontalAlignment.Right };
                Grid.SetColumn(val, 2); g.Children.Add(val);
                peekBody.Children.Add(g);
            }

            // Die heißesten Sensoren (max. 5)
            var temps = PeekTemps.Where(kv => !double.IsNaN(kv.Value) && kv.Value > 5 && kv.Value < 130)
                                 .OrderByDescending(kv => kv.Value).Take(5).ToList();
            if (temps.Count > 0) {
                peekBody.Children.Add(new Border { Height = 1, Background = BORDER, Margin = new Thickness(0, 7, 0, 6) });
                foreach (var kv in temps) {
                    var r = new DockPanel { Margin = new Thickness(0, 1, 0, 1) };
                    var v = new TextBlock { Text = FormatTemp(kv.Value), FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = TempBrush(kv.Value) };
                    DockPanel.SetDock(v, Dock.Right); r.Children.Add(v);
                    r.Children.Add(new TextBlock { Text = I18n.T(CuratedName(kv.Key)), FontSize = 12, Foreground = TXT, TextTrimming = TextTrimming.CharacterEllipsis });
                    peekBody.Children.Add(r);
                }
            } else if (PeekFans.Count == 0) {
                peekBody.Children.Add(new TextBlock { Text = I18n.T("Starting…"), FontSize = 12, Foreground = SUB });
            }
        }
    }
}
