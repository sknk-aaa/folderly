using Folderly.App.Services;
using Folderly.App.ViewModels;
using System.Windows;

namespace Folderly.App.Views;

public partial class HelpWindow : Window
{
    private readonly SettingsViewModel _vm = new();

    public HelpWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void Contact_Click(object sender, RoutedEventArgs e)
        => SupportNavigationService.OpenContactForm();

    private void Faq_Click(object sender, RoutedEventArgs e)
        => SupportNavigationService.OpenFaq();

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}
