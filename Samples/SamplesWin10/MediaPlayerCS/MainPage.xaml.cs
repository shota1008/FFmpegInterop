//*****************************************************************************
//
//	Copyright 2015 Microsoft Corporation
//
//	Licensed under the Apache License, Version 2.0 (the "License");
//	you may not use this file except in compliance with the License.
//	You may obtain a copy of the License at
//
//	http ://www.apache.org/licenses/LICENSE-2.0
//
//	Unless required by applicable law or agreed to in writing, software
//	distributed under the License is distributed on an "AS IS" BASIS,
//	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//	See the License for the specific language governing permissions and
//	limitations under the License.
//
//*****************************************************************************

using System;
using FFmpegInterop;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace MediaPlayerCS
{
	public sealed partial class MainPage : Page
    {
        private FFmpegInteropMSS FFmpegMSS;

        public MainPage()
        {
            InitializeComponent();
            Splitter.IsPaneOpen = true;
        }

        private async void OpenLocalFile(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.Thumbnail;
            filePicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            filePicker.FileTypeFilter.Add("*");

            // Show file picker so user can select a file
            var file = await filePicker.PickSingleFileAsync();
            if (file == null)
            {
                return;
            }

            mediaElement.Stop();

            var stream = await file.OpenAsync(FileAccessMode.Read);
            bool forceDecodeAudio = toggleSwitchAudioDecode.IsOn;
            bool forceDecodeVideo = toggleSwitchVideoDecode.IsOn;

            try
            {
                FFmpegMSS = FFmpegInteropMSS.CreateFFmpegInteropMSSFromStream(stream, forceDecodeAudio, forceDecodeVideo);
                if (FFmpegMSS == null)
                {
                    DisplayErrorMessage("Cannot open media");
                    return;
                }

                var mss = FFmpegMSS.GetMediaStreamSource();
                if (mss == null)
                {
                    DisplayErrorMessage("Cannot open media");
                    return;
                }

                mediaElement.SetMediaStreamSource(mss);
                Splitter.IsPaneOpen = false;
            }
            catch (Exception ex)
            {
                DisplayErrorMessage(ex.Message);
            }
        }

        private void URIBoxKeyUp(object sender, KeyRoutedEventArgs e)
        {
            string uri = (sender as TextBox).Text;

            // Only respond when the text box is not empty and after Enter key is pressed
            if (e.Key == VirtualKey.Enter && !string.IsNullOrWhiteSpace(uri))
            {
                // Mark event as handled to prevent duplicate event to re-triggered
                e.Handled = true;

                bool forceDecodeAudio = toggleSwitchAudioDecode.IsOn;
                bool forceDecodeVideo = toggleSwitchVideoDecode.IsOn;

                // Set FFmpeg specific options. List of options can be found in https://www.ffmpeg.org/ffmpeg-protocols.html
                var options = new PropertySet();

                // Below are some sample options that you can set to configure RTSP streaming
                // options.Add("rtsp_flags", "prefer_tcp");
                // options.Add("stimeout", 100000);

                // http options
                options.Add("user_agent", "MediaPlayerCS");

                try
                {
                    mediaElement.Stop();

                    FFmpegMSS = FFmpegInteropMSS.CreateFFmpegInteropMSSFromUri(uri, forceDecodeAudio, forceDecodeVideo, options);
                    if (FFmpegMSS == null)
                    {
                        DisplayErrorMessage("Cannot open media");
                        return;
                    }

                    var mss = FFmpegMSS.GetMediaStreamSource();
                    if (mss == null)
                    {
                        DisplayErrorMessage("Cannot open media");
                        return;
                    }

                    // Pass MediaStreamSource to Media Element
                    mediaElement.SetMediaStreamSource(mss);
                    Splitter.IsPaneOpen = false;
                }
                catch (Exception ex)
                {
                    DisplayErrorMessage(ex.Message);
                }
            }
        }

        private void MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            DisplayErrorMessage(e.ErrorMessage);
        }

        private async void DisplayErrorMessage(string message)
        {
            var errorDialog = new MessageDialog(message);
            await errorDialog.ShowAsync();
        }
    }
}
