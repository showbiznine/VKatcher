using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace VKatcher.Behaviors
{
    public class VisibilityTransitionBehavior : Behavior<FrameworkElement>
    {

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(Visibility), typeof(VisibilityTransitionBehavior), new PropertyMetadata(default(Visibility), PropertyChangedCallback));

        public Visibility Value

        {
            get { return (Visibility)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var b = (VisibilityTransitionBehavior)d;
            b.TransitionOut((Visibility)e.OldValue);
        }

        public static readonly DependencyProperty AnimationOutProperty =
            DependencyProperty.Register("AnimationOut", typeof(Storyboard), typeof(VisibilityTransitionBehavior), new PropertyMetadata(default(Storyboard)));

        public Storyboard AnimationOut
        {
            get { return (Storyboard)GetValue(AnimationOutProperty); }
            set { SetValue(AnimationOutProperty, value); }
        }

        public static readonly DependencyProperty AnimationInProperty =
            DependencyProperty.Register("AnimationIn", typeof(Storyboard), typeof(VisibilityTransitionBehavior), new PropertyMetadata(default(Storyboard)));

        public Storyboard AnimationIn
        {
            get { return (Storyboard)GetValue(AnimationInProperty); }
            set { SetValue(AnimationInProperty, value); }
        }

        protected override void OnAttached()
        {
            AssociatedObject.Visibility = Value;
            base.OnAttached();
        }

        private void TransitionOut(Visibility oldValue)
        {
            if (AssociatedObject == null)
                return;
            if (AnimationOut == null || oldValue == Visibility.Collapsed)
            {
                TransitionIn();
            }
            else
            {
                AnimationOut.Completed += AnimationOutCompleted;
                AnimationOut.Begin();
            }
        }

        private void TransitionIn()
        {
            if (AssociatedObject == null)
                return;
            AssociatedObject.Visibility = Value;
            if (AnimationIn != null)
            {
                AnimationIn.Begin();
            }
        }

        void AnimationOutCompleted(object sender, object e)
        {
            AnimationOut.Completed -= AnimationOutCompleted;
            TransitionIn();
        }
    }
}
