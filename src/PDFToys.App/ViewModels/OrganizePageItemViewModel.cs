namespace PDFToys.App.ViewModels;

public sealed class OrganizePageItemViewModel : ViewModelBase
{
    private int _rotationDegrees;
    private bool _isIncluded = true;
    private int _displayPosition;

    public OrganizePageItemViewModel(int sourcePageNumber, int displayPosition)
    {
        SourcePageNumber = sourcePageNumber;
        _displayPosition = displayPosition;
    }

    public int SourcePageNumber { get; }

    public int DisplayPosition
    {
        get => _displayPosition;
        set
        {
            if (_displayPosition == value)
            {
                return;
            }

            _displayPosition = value;
            OnPropertyChanged();
        }
    }

    public int RotationDegrees
    {
        get => _rotationDegrees;
        set
        {
            var normalized = ((value % 360) + 360) % 360;
            if (_rotationDegrees == normalized)
            {
                return;
            }

            _rotationDegrees = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RotationLabel));
        }
    }

    public string RotationLabel => $"{RotationDegrees}°";

    public bool IsIncluded
    {
        get => _isIncluded;
        set
        {
            if (_isIncluded == value)
            {
                return;
            }

            _isIncluded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IncludedLabel));
        }
    }

    public string IncludedLabel => IsIncluded ? "Yes" : "No";
}
