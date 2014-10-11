using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace LookHub
{
    public interface ICommandHandler
    {
        void LookHub();
        void CopyGithubLink();
    }

    [Export(typeof(ICommandHandler))]
    public class CommandHandler : ICommandHandler
    {
        private readonly DTE _envDte;
        private readonly SVsOutputWindow _outWindow;

        [Import]
        public ILogger Logger { get; set; }

        public CommandHandler(DTE envDte, SVsOutputWindow outWindow)
        {
            _envDte = envDte;
            _outWindow = outWindow;
        }

        public void LookHub()
        {
            var url = GetUrl();

            if (!string.IsNullOrEmpty(url))
            {
                System.Diagnostics.Process.Start(url);
            }
            else
            {
                Output("\nGitHub url could not retrieved.");
            }
        }

        public void CopyGithubLink()
        {
            string url = GetUrl();
            if (!string.IsNullOrEmpty(url))
            {
                Clipboard.SetText(url);
                Output("\nGitHub url successfully copied to clipboard");
            }
            else
            {
                Clipboard.SetText("");
                //TODO: Error pane.
                Output("\nGitHub url could not be copied to clipboard");
            }
        }

        private string GetUrl()
        {
            var doc = _envDte.ActiveDocument;
            var rootProjectItem = _envDte.SelectedItems.OfType<SelectedItem>().First().ProjectItem;
            string fileName = rootProjectItem.Properties.Item("FullPath").Value.ToString();
            var dir = GetGitDir(fileName);
            var file = new GitConfigFile();
            file.LoadFile(Path.Combine(dir, "config"));

            string head = File.ReadAllText(Path.Combine(dir, "HEAD")).Trim();
            string branch;
            string remote = null;
            if (head.StartsWith("ref: refs/heads/"))
            {
                branch = head.Substring(16);
                var sec = file.Sections.FirstOrDefault(s => s.Type == "branch" && s.Name == branch);
                if (sec != null)
                    remote = sec.GetValue("remote");
            }
            else
            {
                branch = head;
            }
            if (string.IsNullOrEmpty(remote))
                remote = "origin";
            var rref = file.Sections.FirstOrDefault(s => s.Type == "remote" && s.Name == remote);
            if (rref == null)
                return null;

            var url = rref.GetValue("url");
            if (url.EndsWith(".git"))
                url = url.Substring(0, url.Length - 4);

            string host;

            int k = url.IndexOfAny(new[] { ':', '@' });
            if (k != -1 && url[k] == '@')
            {
                k++;
                int i = url.IndexOf(':', k);
                if (i != -1)
                    host = url.Substring(k, i - k);
                else
                    return null;
            }
            else
            {
                Uri uri;
                if (Uri.TryCreate(url, UriKind.Absolute, out uri))
                    host = uri.Host;
                else
                    return null;
            }

            int j = url.IndexOf(host);
            var repo = url.Substring(j + host.Length + 1);

            DirectoryInfo directory = new FileInfo(_envDte.Solution.FullName).Directory;

            if (directory != null)
            {
                string subdir = fileName.Substring(directory.FullName.Length + 1);
                subdir = subdir.Replace('\\', '/');

                string tline = "0";
                if (doc != null)
                {
                    TextSelection textSelection = (TextSelection)_envDte.ActiveDocument.Selection;
                    int lineIndex = textSelection.ActivePoint.Line;
                    tline = lineIndex.ToString(CultureInfo.InvariantCulture);
                }

                return "https://" + host + "/" + repo + "/blob/" + branch + "/" + subdir + "#L" + tline;
            }

            return null;
        }

        private void Output(string msg)
        {
            // Get the output window
            //var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var outputWindow = _outWindow as IVsOutputWindow;

            // Ensure that the desired pane is visible
            var paneGuid = VSConstants.OutputWindowPaneGuid.GeneralPane_guid;

            if (outputWindow != null)
            {
                outputWindow.CreatePane(paneGuid, "General", 1, 0);
                IVsOutputWindowPane pane;
                outputWindow.GetPane(paneGuid, out pane);
                // Output the message
                pane.OutputString(msg);
                pane.Activate(); // Brings this pane into view
            }

            Logger.Log("ASD");
        }

        private static string GetGitDir(string subdir)
        {
            var r = Path.GetPathRoot(subdir);

            while (!string.IsNullOrEmpty(subdir) && subdir != r)
            {
                var gd = Path.Combine(subdir, ".git");
                if (Directory.Exists(gd))
                {
                    return gd;
                }

                subdir = Path.GetDirectoryName(subdir);
            }

            return null;
        }
    }
}