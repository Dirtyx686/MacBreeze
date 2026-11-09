// MacBreeze - Smarter Systemmonitor
// Teil von MacBreeze (basiert auf RPMac von golirino) - GPL-2.0-only. Siehe LICENSE und NOTICE.
//
// Eigene Seite "System": CPU, Arbeitsspeicher, Datenträger, Netzwerk und Akku live,
// mit Verlauf. Der "smarte" Teil verbindet die Temperatur mit der Last: Er zeigt, welche
// Programme gerade die CPU belasten (und damit die Hitze erzeugen), und gibt einen Hinweis.
//
// Alles ohne zusätzliche DLLs: GetSystemTimes / GlobalMemoryStatusEx (kernel32),
// NetworkInterface (System.dll), Process (System.dll), PowerStatus (WinForms).
// Gemessen wird auf einem Hintergrund-Thread, die Oberfläche nur aktualisiert.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MacBreeze {

    public partial class MainWindow {

        // ---- Messung ---------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)] struct FILETIME_ { public uint Low, High; public ulong V { get { return ((ulong)High << 32) | Low; } } }
        [DllImport("kernel32.dll")] static extern bool GetSystemTimes(out FILETIME_ idle, out FILETIME_ kernel, out FILETIME_ user);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        class MEMSTAT {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMSTAT));
            public uint dwMemoryLoad; public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)] static extern bool GlobalMemoryStatusEx([In, Out] MEMSTAT m);

        class ProcUse { public string Name; public double Cpu; public long MemMB; public int Count; }

        class SysSample {
            public double Cpu = double.NaN, Ram = double.NaN, RamUsedGB, RamTotalGB;
            public double NetDown, NetUp;           // KB/s
            public double DiskFreePct = double.NaN; public string DiskText = "";
            public int Battery = -1; public bool OnAc = true;
            public List<ProcUse> Top = new List<ProcUse>();
            public int ProcCount; public TimeSpan Uptime;
        }

        ulong smIdle, smKernel, smUser;
        long smRx = -1, smTx = -1; DateTime smNetT;
        readonly Dictionary<int, TimeSpan> smProcPrev = new Dictionary<int, TimeSpan>();
        DateTime smProcT;
        System.Threading.Timer smTimer;
        volatile bool smBusy;

        SysSample MeasureSystem(bool withProcs) {
            var s = new SysSample();
            // CPU gesamt: Anteil Nicht-Leerlauf zwischen zwei Messungen
            try {
                FILETIME_ i, k, u;
                if (GetSystemTimes(out i, out k, out u)) {
                    ulong di = i.V - smIdle, dk = k.V - smKernel, du = u.V - smUser;
                    if (smKernel != 0 && dk + du > 0) s.Cpu = Math.Max(0, Math.Min(100, (1.0 - (double)di / (dk + du)) * 100));
                    smIdle = i.V; smKernel = k.V; smUser = u.V;
                }
            } catch { }
            // Arbeitsspeicher
            try {
                var m = new MEMSTAT();
                if (GlobalMemoryStatusEx(m)) {
                    s.RamTotalGB = m.ullTotalPhys / 1073741824.0;
                    s.RamUsedGB = (m.ullTotalPhys - m.ullAvailPhys) / 1073741824.0;
                    s.Ram = s.RamTotalGB > 0 ? s.RamUsedGB / s.RamTotalGB * 100 : double.NaN;
                }
            } catch { }
            // Netzwerk: Summe aller aktiven Adapter (ohne Loopback/Tunnel)
            try {
                long rx = 0, tx = 0;
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()) {
                    if (ni.OperationalStatus != System.Net.NetworkInformation.OperationalStatus.Up) continue;
                    var t = ni.NetworkInterfaceType;
                    if (t == System.Net.NetworkInformation.NetworkInterfaceType.Loopback || t == System.Net.NetworkInformation.NetworkInterfaceType.Tunnel) continue;
                    var st = ni.GetIPv4Statistics();
                    rx += st.BytesReceived; tx += st.BytesSent;
                }
                var now = DateTime.UtcNow;
                if (smRx >= 0) {
                    double sec = Math.Max(0.2, (now - smNetT).TotalSeconds);
                    s.NetDown = Math.Max(0, (rx - smRx) / 1024.0 / sec);
                    s.NetUp = Math.Max(0, (tx - smTx) / 1024.0 / sec);
                }
                smRx = rx; smTx = tx; smNetT = now;
            } catch { }
            // Systemlaufwerk
            try {
                var d = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(Environment.SystemDirectory));
                if (d.IsReady && d.TotalSize > 0) {
                    s.DiskFreePct = 100.0 * d.AvailableFreeSpace / d.TotalSize;
                    s.DiskText = string.Format(I18n.T("{0:0} GB free of {1:0} GB"), d.AvailableFreeSpace / 1073741824.0, d.TotalSize / 1073741824.0);
                }
            } catch { }
            // Akku
            try {
                var ps = System.Windows.Forms.SystemInformation.PowerStatus;
                if (ps.BatteryChargeStatus != System.Windows.Forms.BatteryChargeStatus.NoSystemBattery && ps.BatteryLifePercent <= 1.0f)
                    s.Battery = (int)Math.Round(ps.BatteryLifePercent * 100);
                s.OnAc = ps.PowerLineStatus != System.Windows.Forms.PowerLineStatus.Offline;
            } catch { }
            try { s.Uptime = TimeSpan.FromMilliseconds(Environment.TickCount & int.MaxValue); } catch { }

            // Top-Prozesse nach CPU (nur wenn gebraucht: kostet ein paar ms)
            if (withProcs) {
                try {
                    var now = DateTime.UtcNow;
                    double wall = (now - smProcT).TotalMilliseconds * Environment.ProcessorCount;
                    var byName = new Dictionary<string, ProcUse>(StringComparer.OrdinalIgnoreCase);
                    var seen = new HashSet<int>();
                    var procs = Process.GetProcesses();
                    s.ProcCount = procs.Length;
                    foreach (var p in procs) {
                        try {
                            if (p.Id == 0 || p.Id == 4) continue;           // Leerlauf / System
                            TimeSpan cpu = p.TotalProcessorTime;
                            seen.Add(p.Id);
                            TimeSpan prev;
                            double pct = 0;
                            if (smProcPrev.TryGetValue(p.Id, out prev) && wall > 0)
                                pct = (cpu - prev).TotalMilliseconds / wall * 100;
                            smProcPrev[p.Id] = cpu;
                            string n = p.ProcessName;
                            ProcUse pu;
                            if (!byName.TryGetValue(n, out pu)) { pu = new ProcUse { Name = n }; byName[n] = pu; }
                            pu.Cpu += Math.Max(0, pct); pu.Count++;
                            try { pu.MemMB += p.WorkingSet64 / 1048576; } catch { }
                        } catch { } // Zugriff verweigert (geschützte Prozesse)
                        finally { try { p.Dispose(); } catch { } }
                    }
                    foreach (var id in smProcPrev.Keys.ToList()) if (!seen.Contains(id)) smProcPrev.Remove(id);
                    bool first = smProcT == default(DateTime);
                    smProcT = now;
                    if (!first) s.Top = byName.Values.OrderByDescending(x => x.Cpu).ThenByDescending(x => x.MemMB).Take(6).ToList();
                } catch { }
            }
            return s;
        }

        // ---- Oberfläche ------------------------------------------------------------
        class Tile { public TextBlock Value, Sub; public Polyline Spark; public Queue<double> Hist = new Queue<double>(); public double Max = 100; }
        Tile tCpu, tRam, tNet, tDisk, tBat;
        StackPanel smTopList; TextBlock smHint, smHintTitle, smInfo; Border smHintCard;
        List<ProcUse> lastTop = new List<ProcUse>();

        // Beim Öffnen der Seite sofort die letzte Prozessliste zeigen (nicht erst nach 2 s)
        internal void SystemPageShown() {
            if (smTopList == null || lastTop.Count == 0) return;
            smTopList.Children.Clear();
            double maxCpu = Math.Max(1, lastTop[0].Cpu);
            foreach (var p in lastTop) smTopList.Children.Add(ProcRow(p, maxCpu));
        }
        const int SPARK_N = 60; const double SPARK_W = 190, SPARK_H = 38;

        // Werte, die die Lüfter-Schleife liefert (UI-Thread)
        internal double SmHot = double.NaN; internal string SmHotName = "";
        internal double SmFanFrac = double.NaN;

        Tile MakeTile(Panel host, string title, string unitHint) {
            var t = new Tile();
            var col = new StackPanel();
            col.Children.Add(SectionLabel(title, 0));
            t.Value = new TextBlock { Text = "—", FontSize = 26, FontWeight = FontWeights.Bold, Foreground = TXT };
            col.Children.Add(t.Value);
            t.Sub = new TextBlock { Text = unitHint, FontSize = 11, Foreground = SUB, Margin = new Thickness(0, 1, 0, 8), TextTrimming = TextTrimming.CharacterEllipsis };
            col.Children.Add(t.Sub);
            var cv = new Canvas { Width = SPARK_W, Height = SPARK_H, ClipToBounds = true, HorizontalAlignment = HorizontalAlignment.Left };
            t.Spark = new Polyline { Stroke = Look.Fill(ACCENT), StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round };
            cv.Children.Add(t.Spark);
            col.Children.Add(cv);
            var card = Card(col);
            card.Width = 232;
            host.Children.Add(card);
            return t;
        }

        void PushTile(Tile t, double v) {
            if (double.IsNaN(v)) return;
            t.Hist.Enqueue(v); while (t.Hist.Count > SPARK_N) t.Hist.Dequeue();
            double max = t.Max;
            if (max <= 0) { max = 1; foreach (var h in t.Hist) if (h > max) max = h; max *= 1.15; }
            var pts = new PointCollection();
            int n = t.Hist.Count, i = 0;
            if (n == 1) { double v1 = 0; foreach (var h in t.Hist) v1 = h; double y1 = SPARK_H - 2 - Math.Min(1, v1 / max) * (SPARK_H - 4); pts.Add(new Point(0, y1)); pts.Add(new Point(SPARK_W, y1)); }
            else foreach (var h in t.Hist) {
                pts.Add(new Point(i * SPARK_W / (n - 1), SPARK_H - 2 - Math.Min(1, h / max) * (SPARK_H - 4)));
                i++;
            }
            t.Spark.Points = pts;
        }

        static string Rate(double kb) {
            if (kb >= 1024) return (kb / 1024).ToString("0.0") + " MB/s";
            return kb.ToString("0") + " KB/s";
        }

        void BuildSystemPage(Panel parent) {
            var head = new StackPanel { Margin = new Thickness(6, 0, 6, 4) };
            head.Children.Add(new TextBlock { Text = I18n.T("System monitor"), FontSize = 15, FontWeight = FontWeights.Bold, Foreground = TXT });
            smInfo = new TextBlock { Text = I18n.T("Measuring…"), FontSize = 11, Foreground = SUB, Margin = new Thickness(0, 2, 0, 4) };
            head.Children.Add(smInfo);
            parent.Children.Add(head);

            // Smarter Hinweis ganz oben: was heizt gerade?
            var hcol = new StackPanel();
            smHintTitle = new TextBlock { Text = I18n.T("All good"), FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = TXT };
            smHint = new TextBlock { Text = "", FontSize = 12, Foreground = SUB, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0) };
            hcol.Children.Add(smHintTitle); hcol.Children.Add(smHint);
            smHintCard = Card(hcol);
            parent.Children.Add(smHintCard);

            var tiles = new WrapPanel { Orientation = Orientation.Horizontal };
            tCpu = MakeTile(tiles, I18n.T("Processor"), I18n.T("Load"));
            tRam = MakeTile(tiles, I18n.T("Memory (RAM)"), "");
            tNet = MakeTile(tiles, I18n.T("Network"), ""); tNet.Max = 0;   // automatische Skala
            tDisk = MakeTile(tiles, I18n.T("System drive"), "");
            tBat = MakeTile(tiles, I18n.T("Battery"), "");
            parent.Children.Add(tiles);

            var pcol = new StackPanel();
            pcol.Children.Add(SectionLabel(I18n.T("What is causing load right now"), 0));
            smTopList = new StackPanel();
            smTopList.Children.Add(new TextBlock { Text = I18n.T("Measuring…"), Foreground = SUB, FontSize = 12 });
            pcol.Children.Add(smTopList);
            parent.Children.Add(Card(pcol));
        }

        // Messung läuft alle 2 s im Hintergrund (auch für den Hinweis bei Hitze).
        void StartSystemMonitor() {
            smTimer = new System.Threading.Timer(delegate {
                if (smBusy || !running) return;
                smBusy = true;
                try {
                    bool visible = false;
                    try { Dispatcher.Invoke((Action)delegate { visible = IsVisible && currentPage == "system"; }); } catch { return; }
                    var s = MeasureSystem(true);   // immer: die CPU-Anteile brauchen zwei Messungen in Folge
                    Dispatcher.BeginInvoke((Action)delegate { try { ShowSystem(s, visible); } catch { } });
                } catch { }
                finally { smBusy = false; }
            }, null, 500, 2000);
        }

        void ShowSystem(SysSample s, bool visible) {
            // Werte immer in den Verlauf, sichtbar machen nur wenn die Seite offen ist
            if (!double.IsNaN(s.Cpu)) PeekCpu = s.Cpu;
            PushTile(tCpu, s.Cpu); PushTile(tRam, s.Ram); PushTile(tNet, s.NetDown + s.NetUp);
            PushTile(tDisk, double.IsNaN(s.DiskFreePct) ? double.NaN : 100 - s.DiskFreePct);
            if (s.Battery >= 0) PushTile(tBat, s.Battery);

            UpdateHint(s);

            tCpu.Value.Text = double.IsNaN(s.Cpu) ? "—" : s.Cpu.ToString("0") + " %";
            tCpu.Value.Foreground = s.Cpu >= 85 ? RED : TXT;
            tCpu.Sub.Text = string.Format(I18n.T("{0} threads"), Environment.ProcessorCount) + (double.IsNaN(SmHot) ? "" : " · " + FormatTemp(SmHot));
            tRam.Value.Text = double.IsNaN(s.Ram) ? "—" : s.Ram.ToString("0") + " %";
            tRam.Value.Foreground = s.Ram >= 90 ? RED : TXT;
            tRam.Sub.Text = string.Format(I18n.T("{0:0.0} of {1:0.0} GB used"), s.RamUsedGB, s.RamTotalGB);
            tNet.Value.Text = "↓ " + Rate(s.NetDown);
            tNet.Sub.Text = "↑ " + Rate(s.NetUp);
            tDisk.Value.Text = double.IsNaN(s.DiskFreePct) ? "—" : string.Format(I18n.T("{0:0} % used"), 100 - s.DiskFreePct);
            tDisk.Value.Foreground = (!double.IsNaN(s.DiskFreePct) && s.DiskFreePct < 10) ? RED : TXT;
            tDisk.Sub.Text = s.DiskText;
            if (s.Battery >= 0) {
                tBat.Value.Text = s.Battery + " %";
                tBat.Value.Foreground = (!s.OnAc && s.Battery <= 15) ? RED : TXT;
                tBat.Sub.Text = s.OnAc ? I18n.T("plugged in") : I18n.T("on battery");
            } else { tBat.Value.Text = "—"; tBat.Sub.Text = I18n.T("no battery"); }

            smInfo.Text = string.Format(I18n.T("{0} processes · up for {1}"), s.ProcCount > 0 ? s.ProcCount.ToString() : "—", Dauer(s.Uptime));

            lastTop = s.Top;
            if (visible && s.Top.Count > 0) {
                smTopList.Children.Clear();
                double maxCpu = Math.Max(1, s.Top[0].Cpu);
                foreach (var p in s.Top) smTopList.Children.Add(ProcRow(p, maxCpu));
            }
        }

        static string Dauer(TimeSpan t) {
            if (t.TotalDays >= 1) return string.Format(I18n.T("{0} d {1} h"), (int)t.TotalDays, t.Hours);
            if (t.TotalHours >= 1) return string.Format(I18n.T("{0} h {1} min"), (int)t.TotalHours, t.Minutes);
            return string.Format(I18n.T("{0} min"), (int)t.TotalMinutes);
        }

        UIElement ProcRow(ProcUse p, double maxCpu) {
            var g = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            var name = new TextBlock { Text = p.Name + (p.Count > 1 ? "  ×" + p.Count : ""), Foreground = TXT, FontSize = 12.5, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            g.Children.Add(name);
            var track = new Border { Background = CHIP, CornerRadius = new CornerRadius(3), Height = 6, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
            var fill = new Border { Background = p.Cpu >= 50 ? (Brush)RED : Look.Fill(ACCENT), CornerRadius = new CornerRadius(3), HorizontalAlignment = HorizontalAlignment.Left, Height = 6 };
            track.Child = fill;
            track.SizeChanged += delegate { fill.Width = Math.Max(2, track.ActualWidth * Math.Min(1, p.Cpu / maxCpu)); };
            Grid.SetColumn(track, 1); g.Children.Add(track);
            var cpu = new TextBlock { Text = p.Cpu.ToString("0.0") + " %", Foreground = p.Cpu >= 50 ? RED : TXT, FontSize = 12.5, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(cpu, 2); g.Children.Add(cpu);
            var mem = new TextBlock { Text = p.MemMB + " MB", Foreground = SUB, FontSize = 11.5, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(mem, 3); g.Children.Add(mem);
            return g;
        }

        // Der smarte Teil: Temperatur + Last + Lüfter zusammen deuten.
        void UpdateHint(SysSample s) {
            if (smHint == null) return;
            ProcUse top = s.Top.Count > 0 ? s.Top[0] : null;
            bool hot = !double.IsNaN(SmHot) && SmHot >= 85, warm = !double.IsNaN(SmHot) && SmHot >= 75;
            bool busy = !double.IsNaN(s.Cpu) && s.Cpu >= 60;
            bool fansHigh = !double.IsNaN(SmFanFrac) && SmFanFrac >= 0.85;
            string title, text; Brush col = TXT;

            if (hot && top != null && top.Cpu >= 15) {
                title = string.Format(I18n.T("Hot: {0} – main cause: {1}"), FormatTemp(SmHot), top.Name);
                text = string.Format(I18n.T("{0} is using {1:0} % CPU right now. Closing or pausing it lowers the temperature fastest."), top.Name, top.Cpu)
                     + " " + (fansHigh ? I18n.T("The fans are already close to maximum.") : I18n.T("Alternatively set the fans to “Max”."));
                col = RED;
            } else if (hot) {
                title = string.Format(I18n.T("Hot: {0}"), FormatTemp(SmHot)) + (SmHotName != "" ? " (" + SmHotName + ")" : "");
                text = busy ? string.Format(I18n.T("High overall load ({0:0} % CPU), spread across many programs. Setting the fans to “Max” or using a curve helps."), s.Cpu)
                            : I18n.T("The CPU is barely busy, yet it is hot. Possible causes: dusty fans, blocked vents or a soft surface.");
                col = RED;
            } else if (warm && busy && top != null) {
                title = string.Format(I18n.T("Warm under load ({0})"), FormatTemp(SmHot));
                text = string.Format(I18n.T("Biggest consumer: {0} with {1:0} % CPU. For sustained load a fan curve is worth it."), top.Name, top.Cpu);
                col = WARN;
            } else if (!double.IsNaN(s.Ram) && s.Ram >= 90) {
                title = string.Format(I18n.T("Memory almost full ({0:0} %)"), s.Ram);
                text = I18n.T("Windows is now swapping to the SSD, which slows things down. Closing large programs or browser tabs helps.");
                col = WARN;
            } else if (!double.IsNaN(s.DiskFreePct) && s.DiskFreePct < 10) {
                title = I18n.T("System drive almost full");
                text = s.DiskText + ". " + I18n.T("Below 10 % free space, updates and swapping become slow.");
                col = WARN;
            } else if (s.Battery >= 0 && !s.OnAc && s.Battery <= 15) {
                title = string.Format(I18n.T("Battery low ({0} %)"), s.Battery);
                text = I18n.T("Plug in the charger. High fan speeds drain extra battery.");
                col = WARN;
            } else {
                title = I18n.T("All good");
                text = (double.IsNaN(SmHot) ? "" : I18n.T("Temperature") + " " + FormatTemp(SmHot) + " · ")
                     + (double.IsNaN(s.Cpu) ? "" : "CPU " + s.Cpu.ToString("0") + " % · ")
                     + (double.IsNaN(s.Ram) ? "" : "RAM " + s.Ram.ToString("0") + " %");
                col = GOOD;
            }
            smHintTitle.Text = title; smHintTitle.Foreground = col; smHint.Text = text;
            smHintCard.BorderBrush = (col == TXT || col == GOOD) ? BORDER : col;
        }
    }
}
