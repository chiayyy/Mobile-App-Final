using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Networking;
using SOSEmergency.Models;
using SOSEmergency.Services;
using System.Text.RegularExpressions;

namespace SOSEmergency;

public partial class MainPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private string _currentLatitude = "";
    private string _currentLongitude = "";
    private string _currentNetworkStatus = "";
    private bool _isValidTripId = false;

    public MainPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await GetLocationAndConnectivity();
        await UpdateRecordCount();
    }

    private async Task GetLocationAndConnectivity()
    {
        CheckConnectivity();
        await GetCurrentLocation();
    }

    private void CheckConnectivity()
    {
        try
        {
            var connectivity = Connectivity.Current;
            var networkAccess = connectivity.NetworkAccess;

            switch (networkAccess)
            {
                case NetworkAccess.Internet:
                    ConnectivityLabel.Text = "Connected";
                    _currentNetworkStatus = "Connected";
                    StatusIndicator.BackgroundColor = Color.FromArgb("#00ff88");
                    break;
                case NetworkAccess.ConstrainedInternet:
                    ConnectivityLabel.Text = "Limited Connection";
                    _currentNetworkStatus = "Limited";
                    StatusIndicator.BackgroundColor = Color.FromArgb("#ffd700");
                    break;
                case NetworkAccess.Local:
                    ConnectivityLabel.Text = "Local Network Only";
                    _currentNetworkStatus = "Local";
                    StatusIndicator.BackgroundColor = Color.FromArgb("#ffd700");
                    break;
                case NetworkAccess.None:
                    ConnectivityLabel.Text = "No Connection";
                    _currentNetworkStatus = "Offline";
                    StatusIndicator.BackgroundColor = Color.FromArgb("#ff4444");
                    break;
                default:
                    ConnectivityLabel.Text = "Unknown";
                    _currentNetworkStatus = "Unknown";
                    StatusIndicator.BackgroundColor = Color.FromArgb("#888888");
                    break;
            }
        }
        catch (Exception ex)
        {
            ConnectivityLabel.Text = "Error";
            _currentNetworkStatus = "Error";
            StatusIndicator.BackgroundColor = Color.FromArgb("#ff4444");
            StatusLabel.Text = $"Connectivity error: {ex.Message}";
        }
    }

    private async Task GetCurrentLocation()
    {
        try
        {
            StatusLabel.Text = "Acquiring GPS location...";
            RefreshButton.IsEnabled = false;

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location != null)
            {
                _currentLatitude = location.Latitude.ToString("F6");
                _currentLongitude = location.Longitude.ToString("F6");

                LatitudeLabel.Text = _currentLatitude;
                LongitudeLabel.Text = _currentLongitude;

                if (location.Accuracy.HasValue)
                {
                    AccuracyLabel.Text = $"{location.Accuracy.Value:F1} m";
                }
                else
                {
                    AccuracyLabel.Text = "N/A";
                }

                TimestampLabel.Text = DateTime.Now.ToString("HH:mm:ss");
                StatusLabel.Text = "Location acquired - Enter Trip ID to save";
                StatusLabel.TextColor = Color.FromArgb("#00ff88");
            }
            else
            {
                _currentLatitude = "";
                _currentLongitude = "";
                LatitudeLabel.Text = "Unavailable";
                LongitudeLabel.Text = "Unavailable";
                StatusLabel.Text = "Unable to retrieve location";
                StatusLabel.TextColor = Color.FromArgb("#ff4444");
            }
        }
        catch (FeatureNotSupportedException)
        {
            LatitudeLabel.Text = "N/A";
            LongitudeLabel.Text = "N/A";
            StatusLabel.Text = "Geolocation not supported on this device";
            StatusLabel.TextColor = Color.FromArgb("#ff4444");
        }
        catch (FeatureNotEnabledException)
        {
            LatitudeLabel.Text = "Disabled";
            LongitudeLabel.Text = "Disabled";
            StatusLabel.Text = "Please enable location services";
            StatusLabel.TextColor = Color.FromArgb("#ffd700");
        }
        catch (PermissionException)
        {
            LatitudeLabel.Text = "Denied";
            LongitudeLabel.Text = "Denied";
            StatusLabel.Text = "Location permission required for SOS";
            StatusLabel.TextColor = Color.FromArgb("#ff4444");
        }
        catch (Exception ex)
        {
            LatitudeLabel.Text = "Error";
            LongitudeLabel.Text = "Error";
            StatusLabel.Text = $"Error: {ex.Message}";
            StatusLabel.TextColor = Color.FromArgb("#ff4444");
        }
        finally
        {
            RefreshButton.IsEnabled = true;
        }
    }

    private void OnTripIdTextChanged(object sender, TextChangedEventArgs e)
    {
        ValidateTripId(e.NewTextValue);
    }

    private void ValidateTripId(string tripId)
    {
        _isValidTripId = false;
        bool rule1Pass = false;
        bool rule2Pass = false;
        bool rule3Pass = false;
        bool rule4Pass = false;

        if (string.IsNullOrEmpty(tripId))
        {
            ResetValidationRules();
            SaveButton.IsEnabled = false;
            TripIdFrame.BackgroundColor = Color.FromArgb("#0f3460");
            ValidationErrorLabel.IsVisible = false;
            return;
        }

        rule1Pass = tripId.Length >= 3;
        UpdateRuleColor(Rule1Label, rule1Pass);

        rule2Pass = tripId.Length <= 20;
        UpdateRuleColor(Rule2Label, rule2Pass);

        rule3Pass = Regex.IsMatch(tripId, @"^[a-zA-Z0-9\-]+$");
        UpdateRuleColor(Rule3Label, rule3Pass);

        rule4Pass = Regex.IsMatch(tripId, @"^[a-zA-Z]");
        UpdateRuleColor(Rule4Label, rule4Pass);

        _isValidTripId = rule1Pass && rule2Pass && rule3Pass && rule4Pass;

        if (_isValidTripId)
        {
            TripIdFrame.BackgroundColor = Color.FromArgb("#1a4d1a");
            ValidationErrorLabel.IsVisible = false;
            SaveButton.IsEnabled = !string.IsNullOrEmpty(_currentLatitude);
        }
        else
        {
            TripIdFrame.BackgroundColor = Color.FromArgb("#4d1a1a");
            SaveButton.IsEnabled = false;

            if (!rule4Pass && tripId.Length > 0)
            {
                ValidationErrorLabel.Text = "Trip ID must start with a letter";
                ValidationErrorLabel.IsVisible = true;
            }
            else if (!rule3Pass)
            {
                ValidationErrorLabel.Text = "Invalid characters detected";
                ValidationErrorLabel.IsVisible = true;
            }
            else if (!rule1Pass)
            {
                ValidationErrorLabel.Text = "Trip ID is too short";
                ValidationErrorLabel.IsVisible = true;
            }
            else if (!rule2Pass)
            {
                ValidationErrorLabel.Text = "Trip ID is too long (max 20 characters)";
                ValidationErrorLabel.IsVisible = true;
            }
            else
            {
                ValidationErrorLabel.IsVisible = false;
            }
        }
    }

    private void UpdateRuleColor(Label ruleLabel, bool passed)
    {
        ruleLabel.TextColor = passed ? Color.FromArgb("#00ff88") : Color.FromArgb("#ff4444");
    }

    private void ResetValidationRules()
    {
        Rule1Label.TextColor = Color.FromArgb("#666666");
        Rule2Label.TextColor = Color.FromArgb("#666666");
        Rule3Label.TextColor = Color.FromArgb("#666666");
        Rule4Label.TextColor = Color.FromArgb("#666666");
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (!_isValidTripId)
        {
            await DisplayAlert("Validation Error", "Please enter a valid Trip ID", "OK");
            return;
        }

        if (string.IsNullOrEmpty(_currentLatitude) || string.IsNullOrEmpty(_currentLongitude))
        {
            await DisplayAlert("Location Error", "Please wait for location to be acquired", "OK");
            return;
        }

        try
        {
            SaveButton.IsEnabled = false;
            StatusLabel.Text = "Saving trip record...";

            var tripRecord = new TripRecord
            {
                TripId = TripIdEntry.Text.Trim(),
                Latitude = _currentLatitude,
                Longitude = _currentLongitude,
                NetworkStatus = _currentNetworkStatus
            };

            await _databaseService.SaveTripRecordAsync(tripRecord);

            TripIdEntry.Text = "";
            StatusLabel.Text = "Trip record saved successfully!";
            StatusLabel.TextColor = Color.FromArgb("#00ff88");

            await UpdateRecordCount();
            await DisplayAlert("Success", $"Trip '{tripRecord.TripId}' saved to local database", "OK");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Save error: {ex.Message}";
            StatusLabel.TextColor = Color.FromArgb("#ff4444");
            await DisplayAlert("Error", $"Failed to save: {ex.Message}", "OK");
        }
        finally
        {
            SaveButton.IsEnabled = _isValidTripId && !string.IsNullOrEmpty(_currentLatitude);
        }
    }

    private async Task UpdateRecordCount()
    {
        try
        {
            int count = await _databaseService.GetRecordCountAsync();
            RecordCountLabel.Text = $"Saved Records: {count}";
        }
        catch
        {
            RecordCountLabel.Text = "Saved Records: --";
        }
    }

    private async void OnViewRecordsClicked(object sender, EventArgs e)
    {
        try
        {
            var records = await _databaseService.GetAllTripRecordsAsync();

            if (records.Count == 0)
            {
                await DisplayAlert("No Records", "No trip records have been saved yet.", "OK");
                return;
            }

            string recordList = "";
            foreach (var record in records.TakeLast(10))
            {
                recordList += $"ID: {record.TripId}\n";
                recordList += $"Location: {record.Coordinates}\n";
                recordList += $"Time: {record.CreatedAt:yyyy-MM-dd HH:mm}\n";
                recordList += $"Network: {record.NetworkStatus}\n\n";
            }

            await DisplayAlert($"Saved Records ({records.Count} total)", recordList, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load records: {ex.Message}", "OK");
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await GetLocationAndConnectivity();
    }
}
