using System;
using System.IO;
using System.Reflection;
using System.Windows.Input;

namespace PDFToys.App.ViewModels;

public sealed class AboutViewModel : ViewModelBase
{
    public AboutViewModel(Action goBackAction)
    {
        GoBackCommand = new DelegateCommand(goBackAction);
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText = version is null ? "0.1.0" : version.ToString();
        NoticePath = Path.Combine(AppContext.BaseDirectory, "NOTICE");
    }

    public string Title => "About PDFToys";

    public string VersionText { get; }

    public string LicenseSummary =>
        "PDFToys is distributed under the Apache License 2.0. See LICENSE in the repository or installer folder.";

    public string NoticePath { get; }

    public string NoticeSummary =>
        File.Exists(NoticePath)
            ? $"Third-party notices: {NoticePath}"
            : "Third-party notices are listed in the NOTICE file shipped with PDFToys.";

    public ICommand GoBackCommand { get; }

    private sealed class DelegateCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();
    }
}
