using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using VK.WindowsPhone.SDK.API.Model;
using VKatcher.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace VKatcher.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NowPlayingPage : Page
    {
        private Compositor _compositor;
        private MediaPlayer _mediaPlayer;
        private ExpressionAnimation _AlbumArtTranslationAnimation;
        private float _parallaxRatio = 0.3f;
        private Visual _AlbumArt;
        private double _margin;

        public NowPlayingPage()
        {
            this.InitializeComponent();
            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            _mediaPlayer = BackgroundMediaPlayer.Current;
            //SetupTimer();
            SetupMargins();
            SetupParalax();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var vm = DataContext as NowPlayingPageViewModel;
            base.OnNavigatedTo(e);
            //if (e.Parameter is List<object>)
            //{
            //    var lst = e.Parameter as List<object>;
            //    vm._currentTrack = new VKAudio();
            //    vm._currentTrack = lst[0] as VKAudio;

            //    vm._currentPlaylist = new ObservableCollection<VKAudio>();
            //    vm._currentPlaylist = lst[1] as ObservableCollection<VKAudio>;
            //}
        }

        private void SetupParalax()
        {
            myScrollViewer.Loaded += (s, e) =>
            {
                image.ImageOpened += (_s, _e) =>
                {
                    var scrollProperties = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(myScrollViewer);
                    _AlbumArt = ElementCompositionPreview.GetElementVisual(image);
                    InitializeParalax(scrollProperties);
                };
            };
        }

        private void InitializeParalax(CompositionPropertySet scrollProperties)
        {
            //
            // Get the visual for the background image, and let it parallax up until the BackgroundPeekSize.
            // BackgroundPeekSize is later defined as the amount of the image to leave showing.
            //
            Compositor comp = scrollProperties.Compositor;

            //
            // If the scrolling is positive (i.e., bouncing), don't translate at all.  Then check to see if
            // we have parallaxed as far as we should go.  If we haven't, keep parallaxing otherwise use
            // the scrolling translation to keep the background stuck with the background peeking out.
            //

            string str = string.Format("scrollingProperties.Translation.Y * 0.3");

            //string str = string.Format("Clamp(scrollingProperties.Translation.Y * 0.3, -{0}, 0)", _margin - 75);

            _AlbumArtTranslationAnimation = comp.CreateExpressionAnimation(str);

            _AlbumArtTranslationAnimation.SetReferenceParameter("scrollingProperties", scrollProperties);

            _AlbumArtTranslationAnimation.SetScalarParameter("ParallaxRatio", _parallaxRatio);

            #region Blur
            //_backgroundBlurAnimation = _compositor.CreateExpressionAnimation(

            //    "Clamp(-scrollingProperties.Translation.Y / (BackgroundPeekSize * .5),0,1)");

            //_backgroundBlurAnimation.SetScalarParameter("Amount", _backgroundScaleAmount);

            //_backgroundBlurAnimation.SetReferenceParameter("scrollingProperties", scrollProperties);



            //_backgroundInverseBlurAnimation = _compositor.CreateExpressionAnimation(

            //    "1-Clamp(-scrollingProperties.Translation.Y / (BackgroundPeekSize * .5),0,1)");

            //_backgroundInverseBlurAnimation.SetScalarParameter("Amount", _backgroundScaleAmount);

            //_backgroundInverseBlurAnimation.SetReferenceParameter("scrollingProperties", scrollProperties); 
            #endregion

            _AlbumArt.StartAnimation("Offset.Y", _AlbumArtTranslationAnimation);
        }

        private void SetupMargins()
        {
            stkTopControls.Loaded += (s, e) =>
            {
                if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var sb = StatusBar.GetForCurrentView();
                    var color = (Color)Application.Current.Resources["SystemAltHighColor"];
                    sb.BackgroundColor = color;
                    _margin = Frame.ActualHeight - stkTopControls.ActualHeight;
                    stkMain.Margin = new Thickness(0, _margin, 0, 0);
                    listView.Height = _margin;
                    listView.Padding = new Thickness(0, 0, 0, 12);
                }
                else
                {
                    _margin = Frame.ActualHeight - stkTopControls.ActualHeight;
                    stkMain.Margin = new Thickness(0, _margin, 0, 0);
                    listView.Height = _margin;
                    listView.Padding = new Thickness(0, 0, 0, 12);
                }
            };
        }

        private void SetupTimer()
        {
            throw new NotImplementedException();
        }
    }
}
