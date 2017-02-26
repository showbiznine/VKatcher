using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace VKatcher.Behaviors
{

    public class HeightBehavior : DependencyObject, IBehavior
    {
        public ImageEx ImageEx
        {
            get { return (ImageEx)GetValue(ImageExProperty); }
            set { SetValue(ImageExProperty, value); }
        }

        public static readonly DependencyProperty ImageExProperty =
            DependencyProperty.Register("ImageEx", typeof(ImageEx), typeof(HeightBehavior), new PropertyMetadata(null));

        public Double Width
        {
            get { return GetValue(WidthProperty) == null ? 0 : (Double)(GetValue(WidthProperty)); }
            set { SetValue(WidthProperty, value); }
        }

        public static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register("Width", typeof(FrameworkElement), typeof(HeightBehavior), new PropertyMetadata(null));

        public Double AspectRatio
        {
            get { return GetValue(AspectRatioProperty) == null ? 0 : (Double)(GetValue(AspectRatioProperty)); }
            set { SetValue(AspectRatioProperty, value); }
        }

        public static readonly DependencyProperty AspectRatioProperty =
            DependencyProperty.Register("AspectRatio", typeof(FrameworkElement), typeof(HeightBehavior), new PropertyMetadata(new PropertyChangedCallback(OnAspectRatioChanged)));

        private static void OnAspectRatioChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ImageEx)d).Width += 0.5;
        }

        public DependencyObject AssociatedObject { get; set; }

        public void Attach(DependencyObject associatedObject)
        {
            this.AssociatedObject = associatedObject;

            var control = (ImageEx)this.AssociatedObject;
            control.Loaded += AssociatedObject_Loaded;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            this.ImageEx.SizeChanged += Image_SizeChanged;

            // force to re-calculate the Height
            this.ImageEx.Width += 0.5;
        }

        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.SetAssociatedObjectsHeight();
        }

        private void SecondItem_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.SetAssociatedObjectsHeight();
        }

        private void SetAssociatedObjectsHeight()
        {
            this.ImageEx.Height = this.ImageEx.ActualWidth * this.AspectRatio;
            //this.ImageEx.Height = this.ImageEx.ActualWidth;
        }

        public void Detach()
        {
            this.ImageEx.SizeChanged -= Image_SizeChanged;

            var control = (ImageEx)this.AssociatedObject;
            control.Loaded -= AssociatedObject_Loaded;
        }
    }
}
