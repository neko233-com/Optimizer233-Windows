using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Optimizer.Shared.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;

namespace Optimizer_Windows.Models;

public sealed class OptimizationOption : INotifyPropertyChanged
{
    private string _actionId = string.Empty;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _badge = string.Empty;
    private OperationRiskLevel _riskLevel;
    private Brush _badgeBackground = new SolidColorBrush(Color.FromArgb(255, 20, 30, 20));
    private Brush _badgeBorder = new SolidColorBrush(Color.FromArgb(255, 40, 74, 40));
    private Brush _badgeForeground = new SolidColorBrush(Color.FromArgb(255, 173, 255, 190));
    private bool _isSelected;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ActionId
    {
        get => _actionId;
        set => SetField(ref _actionId, value);
    }

    public string Title
    {
        get => _title;
        set => SetField(ref _title, value);
    }

    public string Description
    {
        get => _description;
        set => SetField(ref _description, value);
    }

    public string Badge
    {
        get => _badge;
        set => SetField(ref _badge, value);
    }

    public OperationRiskLevel RiskLevel
    {
        get => _riskLevel;
        set => SetField(ref _riskLevel, value);
    }

    public Brush BadgeBackground
    {
        get => _badgeBackground;
        set => SetField(ref _badgeBackground, value);
    }

    public Brush BadgeBorder
    {
        get => _badgeBorder;
        set => SetField(ref _badgeBorder, value);
    }

    public Brush BadgeForeground
    {
        get => _badgeForeground;
        set => SetField(ref _badgeForeground, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
