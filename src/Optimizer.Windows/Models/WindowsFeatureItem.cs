using Microsoft.UI.Xaml.Media;
using Optimizer.Shared.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Optimizer_Windows.Models;

public sealed class WindowsFeatureItem : INotifyPropertyChanged
{
    private bool _isEnabled;
    private bool _isBusy;
    private bool _isAvailable = true;
    private bool _requiresRestart;
    private string _stateText = string.Empty;
    private string _restartText = string.Empty;

    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string AudienceText { get; set; } = string.Empty;

    public string SearchKeywords { get; set; } = string.Empty;

    public OperationRiskLevel RiskLevel { get; set; }

    public string RiskLabel { get; set; } = string.Empty;

    public Brush RiskBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 188, 196));

    public Brush RiskBackground { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 27, 17, 26));

    public Brush RiskBorder { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 69, 32, 44));

    public Brush StateBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 169, 175, 188));

    public Brush StateBackground { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 18, 22, 30));

    public Brush StateBorder { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 39, 32, 43));

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanToggle));
            }
        }
    }

    public bool IsAvailable
    {
        get => _isAvailable;
        set
        {
            if (SetProperty(ref _isAvailable, value))
            {
                OnPropertyChanged(nameof(CanToggle));
            }
        }
    }

    public bool RequiresRestart
    {
        get => _requiresRestart;
        set => SetProperty(ref _requiresRestart, value);
    }

    public string StateText
    {
        get => _stateText;
        set => SetProperty(ref _stateText, value);
    }

    public string RestartText
    {
        get => _restartText;
        set => SetProperty(ref _restartText, value);
    }

    public bool CanToggle => IsAvailable && !IsBusy;

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
