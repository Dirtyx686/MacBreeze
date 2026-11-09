// MacBreeze installer, GPL-2.0-only. No driver or boot-setting changes.
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
class Setup {
    static string Destination { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "MacBreeze"); } }
    static Stream Payload() { return Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"); }
    [STAThread] static int Main(string[] args) {
        if (Array.IndexOf(args, "/verify") >= 0) {
            using (var stream=Payload()) using (var zip=new ZipArchive(stream)) {
                if (zip.GetEntry("MacBreeze.exe")==null || zip.GetEntry("LICENSE.txt")==null) return 2;
                foreach(var entry in zip.Entries) { if(Path.GetFileName(entry.FullName)!=entry.FullName) return 3; using(var input=entry.Open()) input.CopyTo(Stream.Null); }
            }
            return 0;
        }
        Application.EnableVisualStyles();
        if(Array.IndexOf(args,"/uninstall")>=0) return Uninstall();
        var window=new Form { Text="MacBreeze 2.1 Setup", Width=560, Height=370, StartPosition=FormStartPosition.CenterScreen, FormBorderStyle=FormBorderStyle.FixedDialog, MaximizeBox=false };
        var title=new Label { Text="MacBreeze 2.1 installieren", Left=26,Top=24,Width=490,Height=35,Font=new System.Drawing.Font("Segoe UI",17) };
        var info=new Label { Text="Studio · Cockpit · Fokus\n\nInstallation für dieses Windows-Benutzerkonto.\nDie App erkennt die Hardware beim Start. Falls die T2-Einrichtung\nbenötigt wird, fragt sie vor Treiber- und Testsignaturänderungen nach.\nDer Installer ändert keine Treiber oder Startsicherheit.",Left=28,Top=75,Width=490,Height=125 };
        var location=new Label { Text="Ziel: "+Destination,Left=28,Top=208,Width=490,Height=48 };
        var install=new Button { Text="Installieren",Left=350,Top=270,Width=160,Height=38 };
        install.Click+=delegate {
            install.Enabled=false;
            try {
                using(var stream=Payload()) using(var zip=new ZipArchive(stream)) {
                    // A fixed per-user location; only declared payload files are written.
                    Directory.CreateDirectory(Destination);
                    foreach(var entry in zip.Entries) {
                        if(Path.GetFileName(entry.FullName)!=entry.FullName) throw new InvalidDataException("Ungültiger Paketpfad.");
                        string target=Path.Combine(Destination,entry.FullName);
                        using(var input=entry.Open()) using(var output=new FileStream(target,FileMode.Create,FileAccess.Write)) input.CopyTo(output);
                    }
                }
                File.Copy(Assembly.GetExecutingAssembly().Location,Path.Combine(Destination,"Uninstall.exe"),true);
                Shortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"MacBreeze.lnk"));
                using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\MacBreeze")) {
                    key.SetValue("DisplayName","MacBreeze"); key.SetValue("DisplayVersion","2.1");
                    key.SetValue("InstallLocation",Destination); key.SetValue("Publisher","MacBreeze contributors");
                    key.SetValue("UninstallString","\""+Path.Combine(Destination,"Uninstall.exe")+"\" /uninstall");
                    key.SetValue("NoModify",1); key.SetValue("NoRepair",1);
                }
                MessageBox.Show(window,"MacBreeze 2.1 ist installiert.\nStarte es über das Windows-Startmenü.\nBeim ersten App-Start wird eine Administratorabfrage angezeigt.","Installation abgeschlossen");
                window.Close();
            } catch(Exception error) { MessageBox.Show(window,"Installation fehlgeschlagen: "+error.Message+"\n\nBeende eine laufende MacBreeze-Version und versuche es erneut."); install.Enabled=true; }
        };
        window.Controls.AddRange(new Control[]{title,info,location,install}); Application.Run(window); return 0;
    }
    static void Shortcut(string path) {
        var type=Type.GetTypeFromProgID("WScript.Shell"); var shell=Activator.CreateInstance(type);
        var shortcut=type.InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});
        var st=shortcut.GetType(); st.InvokeMember("TargetPath",BindingFlags.SetProperty,null,shortcut,new object[]{Path.Combine(Destination,"MacBreeze.exe")});
        st.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,shortcut,new object[]{Destination});
        st.InvokeMember("Save",BindingFlags.InvokeMethod,null,shortcut,null);
        System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut); System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
    }
    static int Uninstall() {
        if(MessageBox.Show("MacBreeze-Programmdateien entfernen?\n\nBeende zuvor die App und schalte ihren Autostart aus.\nEinstellungen, geplante Aufgaben, PawnIO und Testsignatur bleiben erhalten.","MacBreeze deinstallieren",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes) return 0;
        try {
            using(var stream=Payload()) using(var zip=new ZipArchive(stream)) foreach(var entry in zip.Entries) {
                if(Path.GetFileName(entry.FullName)!=entry.FullName) throw new InvalidDataException();
                string path=Path.Combine(Destination,entry.FullName); if(File.Exists(path))File.Delete(path);
            }
            string shortcut=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"MacBreeze.lnk");if(File.Exists(shortcut))File.Delete(shortcut);
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\MacBreeze",false);
            MessageBox.Show("Programm entfernt. Uninstall.exe bleibt als letzte Datei im Installationsordner und kann nach dem Schließen gelöscht werden. Persönliche Einstellungen bleiben erhalten.");return 0;
        }catch(Exception error){MessageBox.Show("Entfernen fehlgeschlagen: "+error.Message);return 1;}
    }
}
