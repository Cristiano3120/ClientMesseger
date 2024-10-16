﻿using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ClientMesseger
{
    public sealed partial class Settings : Window
    {
        private readonly Home _homeWindow;

        public Settings(Home window)
        {
            InitializeComponent();
            _homeWindow = window;
            ProfilPic.ImageSource = Client.ProfilPicture;
            btnClose.Click += ClientUI.BtnCloseShutdown_Click;
            btnMaximize.Click += ClientUI.BtnMaximize_Click;
            btnMinimize.Click += ClientUI.BtnMinimize_Click;
            TitleBar.MouseLeftButtonDown += ClientUI.Window_MouseLeftButtonDown;
            KeyDown += (object sender, KeyEventArgs args) =>
            {
                if (args.Key == Key.Escape)
                {
                    _homeWindow.Show();
                    Close();
                }
            };
        }

        private void SettingsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedItem = (ListBoxItem)((ListBox)sender).SelectedItem;
                var selectedSettingAsString = selectedItem.Content.ToString()!;

                var words = selectedSettingAsString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    selectedSettingAsString = string.Empty;
                    foreach (var word in words)
                    {
                        selectedSettingAsString += word.Trim();
                    }
                }
                var selectedSetting = Enum.Parse<SettingsEnum>(selectedSettingAsString);

                switch (selectedSetting)
                {
                    case SettingsEnum.Profil:
                        _ = DisplayError.LogAsync("Profil settings panel opend");
                        ChangeActivePanel(ProfilPanel);
                        break;
                    case SettingsEnum.Language:
                        Console.WriteLine("language panel opend");
                        ChangeActivePanel(LanguagePanel);
                        break;
                    case SettingsEnum.PersonalInformation:
                        _ = DisplayError.LogAsync("Personal information panel opend");
                        ChangeActivePanel(PersonalInformationPanel);
                        break;
                }
            }
            catch (ArgumentException ex)
            {
                DisplayError.DisplayBasicErrorInfos(ex, "Settings", "SettingsList_SelectionChanged");
            }
        }

        private void ChangeActivePanel(StackPanel stackPanelToActivate)
        {
            ProfilPanel.Visibility = Visibility.Collapsed;
            LanguagePanel.Visibility = Visibility.Collapsed;
            PersonalInformationPanel.Visibility = Visibility.Collapsed;
            stackPanelToActivate.Visibility = Visibility.Visible;
        }

        #region ProfilPic

        private void Ellipse_MouseEnter(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Hand;
        }

        private void Ellipse_MouseLeave(object sender, MouseEventArgs e)
        {
            Cursor = Cursors.Arrow;
        }

        private void Ellipse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var openFileDiaLogAsnycAsnyc = new OpenFileDialog()
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg"
            };

            if (openFileDiaLogAsnycAsnyc.ShowDialog() == true)
            {
                var selectedFilePath = openFileDiaLogAsnycAsnyc.FileName;
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(selectedFilePath);
                bitmap.EndInit();
                bitmap.Freeze();

                // Downscale the picture if it´s bigger than 130x 130
                BitmapImage finalBitmap;
                if (bitmap.PixelWidth > 130 || bitmap.PixelHeight > 130)
                {
                    MessageBox.Show("Scaling the image down because it was bigger than 130x130 pixels");
                    var scale = Math.Min(130.0 / bitmap.PixelWidth, 130.0 / bitmap.PixelHeight);
                    var scaledBitmap = new TransformedBitmap(bitmap, new ScaleTransform(scale, scale));
                    finalBitmap = ConvertToBitmapImage(scaledBitmap);
                }
                else
                {
                    finalBitmap = bitmap;
                }

                var base64Image = ConvertToBase64(finalBitmap);
                var payload = new
                {
                    code = 15,
                    id = Client.Id,
                    base64Image,
                };
                var jsonString = JsonSerializer.Serialize(payload);
                _ = Client.SendPayloadAsync(jsonString);

                Client.ProfilPicture = finalBitmap;
                _homeWindow.OnProfilPicChanged(finalBitmap);
                ProfilPic.ImageSource = finalBitmap;
            }
        }

        private static BitmapImage ConvertToBitmapImage(TransformedBitmap transformedBitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(transformedBitmap));
                encoder.Save(memoryStream);
                memoryStream.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        private static string ConvertToBase64(BitmapImage bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(memoryStream);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        #endregion
    }
}
