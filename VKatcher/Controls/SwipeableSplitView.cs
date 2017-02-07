using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;
using Windows.System.Profile;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace VKatcher.Controls
{
    public sealed class SwipeableSplitView : SplitView
    {

        #region Private Fields

        bool _isMobile;

        Compositor _compositor;
        Grid _paneRoot;
        Grid _overlayRoot;
        Rectangle _dismissLayer;
        Rectangle _dimLayer;
        Rectangle _panArea;
        CompositeTransform _paneRootTransform;
        CompositeTransform _panAreaTransform;

        Visual _panAreaVisual;
        Visual _paneRootVisual;
        ScalarKeyFrameAnimation _openAnimation;
        ScalarKeyFrameAnimation _closeAnimation;
        #endregion

        #region Properties

        internal Grid PaneRoot
        {
            get { return _paneRoot; }
            set
            {
                if (_paneRoot != null)
                {
                    _paneRoot.Loaded -= OnPaneRootLoaded;
                    if (_isMobile || OverrideMobileOnly)
                    {
                        _paneRoot.ManipulationStarted -= OnManipulationStarted;
                        _paneRoot.ManipulationDelta -= OnManipulationDelta;
                        _paneRoot.ManipulationCompleted -= OnManipulationCompleted; 
                    }
                }

                _paneRoot = value;

                if (_paneRoot != null)
                {
                    _paneRoot.Loaded += OnPaneRootLoaded;
                    if (_isMobile || OverrideMobileOnly)
                    {
                        _paneRoot.ManipulationStarted += OnManipulationStarted;
                        _paneRoot.ManipulationDelta += OnManipulationDelta;
                        _paneRoot.ManipulationCompleted += OnManipulationCompleted; 
                    }
                }
            }
        }

        internal Rectangle PanArea
        {
            get { return _panArea; }
            set
            {
                if (_panArea != null)
                {
                    _panArea.Loaded -= OnPaneRootLoaded;
                    if (_isMobile || OverrideMobileOnly)
                    {
                        _panArea.ManipulationStarted -= OnManipulationStarted;
                        _panArea.ManipulationDelta -= OnManipulationDelta;
                        _panArea.ManipulationCompleted -= OnManipulationCompleted; 
                    }
                    _panArea.Tapped -= OnDismissLayerTapped;
                }

                _panArea = value;

                if (_panArea != null)
                {
                    _panArea.Loaded += OnPaneRootLoaded;
                    if (_isMobile || OverrideMobileOnly)
                    {
                        _panArea.ManipulationStarted += OnManipulationStarted;
                        _panArea.ManipulationDelta += OnManipulationDelta;
                        _panArea.ManipulationCompleted += OnManipulationCompleted; 
                    }
                    _panArea.Tapped += OnDismissLayerTapped;
                }
            }
        }

        internal Rectangle DismissLayer
        {
            get { return _dismissLayer; }
            set
            {
                if (_dismissLayer != null)
                {
                    _dismissLayer.Tapped -= OnDismissLayerTapped;
                }

                _dismissLayer = value;

                if (_dismissLayer != null)
                {
                    _dismissLayer.Tapped += OnDismissLayerTapped;
                }
            }
        }

        internal Rectangle DimLayer
        {
            get { return _dimLayer; }
            set { _dimLayer = value; }
        }

        public bool IsSwipeablePaneOpen
        {
            get { return (bool)GetValue(IsSwipeablePaneOpenProperty); }
            set { SetValue(IsSwipeablePaneOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSwipeablePaneOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSwipeablePaneOpenProperty =
            DependencyProperty.Register("IsSwipeablePaneOpen", typeof(bool), typeof(SwipeableSplitView), new PropertyMetadata(false, OnIsSwipeablePaneOpenChanged));

        static void OnIsSwipeablePaneOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var splitView = (SwipeableSplitView)d;

            switch (splitView.DisplayMode)
            {
                case SplitViewDisplayMode.Inline:
                case SplitViewDisplayMode.CompactOverlay:
                case SplitViewDisplayMode.CompactInline:
                    splitView.IsPaneOpen = (bool)e.NewValue;
                    break;

                case SplitViewDisplayMode.Overlay:
                    if ((bool)e.NewValue)
                        splitView.OpenSwipeablePane();
                    else
                        splitView.CloseSwipeablePane();
                    break;

            }
        }

        public double PanAreaInitialTranslateX
        {
            get { return (double)GetValue(PanAreaInitialTranslateXProperty); }
            set { SetValue(PanAreaInitialTranslateXProperty, value); }
        }

        public static readonly DependencyProperty PanAreaInitialTranslateXProperty =
            DependencyProperty.Register(nameof(PanAreaInitialTranslateX), typeof(double), typeof(SwipeableSplitView), new PropertyMetadata(0d));

        public double PanAreaThreshold
        {
            get { return (double)GetValue(PanAreaThresholdProperty); }
            set { SetValue(PanAreaThresholdProperty, value); }
        }

        public static readonly DependencyProperty PanAreaThresholdProperty =
            DependencyProperty.Register(nameof(PanAreaThreshold), typeof(double), typeof(SwipeableSplitView), new PropertyMetadata(12d));

        #region BG Dim Properties
        public bool DimBackground
        {
            get { return (bool)GetValue(DimBackgroundProperty); }
            set { SetValue(DimBackgroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DimBackground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DimBackgroundProperty =
            DependencyProperty.Register("DimBackground", typeof(bool), typeof(SwipeableSplitView), new PropertyMetadata(true));


        public double MaxOpacity
        {
            get { return (double)GetValue(MaxOpacityProperty); }
            set { SetValue(MaxOpacityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for MaxOpacity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaxOpacityProperty =
            DependencyProperty.Register("MaxOpacity", typeof(double), typeof(SwipeableSplitView), new PropertyMetadata(0.25));


        #endregion

        //By default, the swipe gesture will only work on mobile, to prevent clashes with the task switcher
        public bool OverrideMobileOnly
        {
            get { return (bool)GetValue(OverrideMobileOnlyProperty); }
            set { SetValue(OverrideMobileOnlyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OverrideMobileOnly.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OverrideMobileOnlyProperty =
            DependencyProperty.Register("OverrideMobileOnly", typeof(bool), typeof(SwipeableSplitView), new PropertyMetadata(false));


        #endregion

        public SwipeableSplitView()
        {
            this.DefaultStyleKey = typeof(SwipeableSplitView);

            switch (AnalyticsInfo.VersionInfo.DeviceFamily)
            {
                case "Windows.Mobile":
                    _isMobile = true;
                    break;
                default:
                    _isMobile = false;
                    break;
            }

            Debug.WriteLine(AnalyticsInfo.VersionInfo.DeviceFamily);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            PaneRoot = GetTemplateChild<Grid>("PaneRoot");
            _overlayRoot = GetTemplateChild<Grid>("OverlayRoot");
            PanArea = GetTemplateChild<Rectangle>("PanArea");
            DismissLayer = GetTemplateChild<Rectangle>("DismissLayer");
            DimLayer = GetTemplateChild<Rectangle>("DimLayer");

            var rootGrid = _paneRoot.GetParent<Grid>();

            OnDisplayModeChanged(null, null);

            RegisterPropertyChangedCallback(DisplayModeProperty, OnDisplayModeChanged);
        }

        void OnPaneRootLoaded(object sender, RoutedEventArgs e)
        {
            _compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
            SetupAnimations();
        }

        void SetupAnimations()
        {
            _paneRootVisual = ElementCompositionPreview.GetElementVisual(PaneRoot);
            _panAreaVisual = ElementCompositionPreview.GetElementVisual(PanArea);
            var dimLayerVisual = ElementCompositionPreview.GetElementVisual(DimLayer);
            var bezierEasing = _compositor.CreateCubicBezierEasingFunction(new Vector2(0.24f, 0.12f), new Vector2(0.18f, 1.01f));

            #region Synced offset

            //Keep the offsets of the pan area and PaneRoot on sync
            var offsetExpressionAnimation = _compositor.CreateExpressionAnimation();
            offsetExpressionAnimation.Expression = "paneRoot.Offset.X";
            offsetExpressionAnimation.SetReferenceParameter("paneRoot", _paneRootVisual);
            _panAreaVisual.StartAnimation("Offset.X", offsetExpressionAnimation);

            #endregion

            #region Synced dim layer

            //Keep the offsets of the pan area and PaneRoot on sync
            if (DimBackground)
            {
                var dimExpressionAnimation = _compositor.CreateExpressionAnimation();

                var propset = _compositor.CreatePropertySet();
                propset.InsertScalar("maxLength", (float)OpenPaneLength);
                propset.InsertScalar("maxOpacity", (float)MaxOpacity);

                dimExpressionAnimation.Expression = "(paneRoot.Offset.X / props.maxLength) * props.maxOpacity";
                dimExpressionAnimation.SetReferenceParameter("paneRoot", _paneRootVisual);
                dimExpressionAnimation.SetReferenceParameter("props", propset);
                dimLayerVisual.StartAnimation("Opacity", dimExpressionAnimation);
            }
            else
                dimLayerVisual.Opacity = 0;

            #endregion

            #region Open Animation
            _openAnimation = _compositor.CreateScalarKeyFrameAnimation();
            _openAnimation.Duration = TimeSpan.FromMilliseconds(300);
            _openAnimation.InsertKeyFrame(1.0f, (float)OpenPaneLength);
            #endregion

            #region Close Animation
            _closeAnimation = _compositor.CreateScalarKeyFrameAnimation();
            _closeAnimation.Duration = TimeSpan.FromMilliseconds(300);
            _closeAnimation.InsertKeyFrame(1.0f, 0f);
            #endregion
        }

        private void OnDisplayModeChanged(object p1, object p2)
        {
            switch (DisplayMode)
            {
                case SplitViewDisplayMode.Inline:
                case SplitViewDisplayMode.CompactOverlay:
                case SplitViewDisplayMode.CompactInline:
                    PanAreaInitialTranslateX = 0d;
                    _overlayRoot.Visibility = Visibility.Collapsed;
                    break;

                case SplitViewDisplayMode.Overlay:
                    PanAreaInitialTranslateX = OpenPaneLength * -1;
                    _overlayRoot.Visibility = Visibility.Visible;
                    break;
            }
        }

        #region Open/Close Pane

        private void OpenSwipeablePane()
        {
            if (IsSwipeablePaneOpen)
            {
                _paneRootVisual.StartAnimation("Offset.X", _openAnimation);
                this.DismissLayer.IsHitTestVisible = true;
            }
            else
            {
                IsSwipeablePaneOpen = true;
            }
        } 

        private void CloseSwipeablePane()
        {
            if (!IsSwipeablePaneOpen)
            {
                _paneRootVisual.StartAnimation("Offset.X", _closeAnimation);
                this.DismissLayer.IsHitTestVisible = false;
            }
            else
            {
                IsSwipeablePaneOpen = false;
            }
        }
        
        #endregion

        #region Manipulation Events

        void OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _panAreaTransform = PanArea.GetCompositeTransform();
            _paneRootTransform = PaneRoot.GetCompositeTransform();
        } 

        void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var x = e.Delta.Translation.X;

            var offset = _paneRootVisual.Offset.X;

            // keep the pan within the bountry
            if (offset < 0) return; 
            if (offset > OpenPaneLength)
            {
                //_paneRootVisual.Offset = new Vector3((float)OpenPaneLength, 0, 0);
                return;
            }       

            _paneRootVisual.Offset = new Vector3(offset + (float)x, 0, 0);
            // while we are panning the PanArea on X axis, let's sync the PaneRoot's position X too
            //_paneRootTransform.TranslateX = _panAreaTransform.TranslateX = x;
        }

        void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            var x = e.Velocities.Linear.X;

            // ignore a little bit velocity (+/-0.1)
            if (x <= -0.1)
            {
                CloseSwipeablePane();
            }
            else if (x > -0.1 && x < 0.1)
            {
                if (Math.Abs(_panAreaTransform.TranslateX) > Math.Abs(PanAreaInitialTranslateX) / 2)
                {
                    CloseSwipeablePane();
                }
                else
                {
                    OpenSwipeablePane();
                }
            }
            else
            {
                OpenSwipeablePane();
            }
        }

        void OnDismissLayerTapped(object sender, TappedRoutedEventArgs e)
        {
            CloseSwipeablePane();
        }

        #endregion

        T GetTemplateChild<T>(string name, string message = null) where T : DependencyObject
        {
            var child = GetTemplateChild(name) as T;
            if (child == null)
            {
                if (message == null)
                {
                    message = $"{name} should not be null! Check the default Generic.xaml.";
                }
                throw new NullReferenceException(message);
            }
            return child;
        }
    }
}
