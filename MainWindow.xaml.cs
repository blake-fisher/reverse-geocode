// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using System;
using System.Linq;
using System.Text.Json;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;

namespace ReverseGeocoder
{
    public sealed partial class MainWindow : Window
    {
        private readonly string AUTH_INSTRUCTIONS = @"This is probably a ServiceToken issue, which you can fix by doing the following:
    1. Go to https://www.bingmapsportal.com/ and sign in.
    2. Go to 'My Account -> My Keys' and create a new key.
    3. Make 'Application name'='ReverseGeocoder', 'Key type'='Basic', and 'Application type'='Windows Application'.
    4. Create the key, and then click 'Copy Key'.
    5. Paste the key into the MapService.ServiceToken='FIXME' field in MainWindow.xaml.cs";
        
        public MainWindow()
        {
            InitializeComponent();

            // This is a dev key created by bfish using the process here: https://learn.microsoft.com/en-us/windows/uwp/maps-and-location/authentication-key#get-a-key
            MapService.ServiceToken = "FIXME";
        }

        private async void ReverseGeocode_Click(object sender, RoutedEventArgs e)
        {
            ErrorBox.Text = "";
            DiffView.Visibility = Visibility.Collapsed;
            DiffView.SetText("", "");

            var latLong = LatLong.Text.Split(',').Select(x => x.Trim()).ToList();
            var location = new BasicGeoposition();
            if (latLong.Count != 2 || !double.TryParse(latLong[0], out location.Latitude) || !double.TryParse(latLong[1], out location.Longitude))
            {
                ErrorBox.Text = $"Invalid location '{LatLong.Text}'";
                return;
            }

            var pointToReverseGeocode = new Geopoint(location);

            var lowAccuracyResult = await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode, MapLocationDesiredAccuracy.Low);
            if (!CheckResult(lowAccuracyResult, "low accuracy"))
            {
                return;
            }
            var highAccuracyResult = await MapLocationFinder.FindLocationsAtAsync(pointToReverseGeocode, MapLocationDesiredAccuracy.High);
            if (!CheckResult(highAccuracyResult, "high accuracy"))
            {
                return;
            }

            SetResult(lowAccuracyResult, highAccuracyResult);
        }

        private bool CheckResult(MapLocationFinderResult result, string resultType)
        {

            if (result.Status != MapLocationFinderStatus.Success)
            {
                ErrorBox.Text = $"Reverse geocode failed for {resultType} result with status '{result.Status}'";

                if (result.Status == MapLocationFinderStatus.InvalidCredentials)
                {
                    ErrorBox.Text += $". {AUTH_INSTRUCTIONS}";
                }

                return false;
            }

            return true;
        }

        private void SetResult(MapLocationFinderResult lowAccuracyResult, MapLocationFinderResult highAccuracyResult)
        {
            DiffView.Visibility = Visibility.Visible;
            DiffView.SetText(
                JsonSerializer.Serialize(lowAccuracyResult.Locations, new JsonSerializerOptions { WriteIndented = true }),
                JsonSerializer.Serialize(highAccuracyResult.Locations, new JsonSerializerOptions { WriteIndented = true })
            );
        }
        
    }
}
