using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace VKatcher.Controls
{
    public sealed partial class NavPaneItem : UserControl
    {
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register("IsSelected", typeof(bool), typeof(NavPaneItem), new PropertyMetadata(false, new PropertyChangedCallback(OnIsSelectedChanged)));

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as NavPaneItem;

            if ((bool)e.NewValue)
            {
                control.AnimateSelected();
            }
            else
            {
                control.AnimateDeselected();
            }
        }

        public NavPaneItem()
        {
            this.InitializeComponent();
        }

        private void NavPaneListItem_Loaded(object sender, RoutedEventArgs e)
        {
            SetupVisualStates();
        }

        private void SetupVisualStates()
        {
            VisualStateGroup group = GetVisualStateGroup("SelectionStates");
            if (group != null)
            {
                group.CurrentStateChanged += (s, args) =>
                {
                    if (group.CurrentState.Name == "Selected")
                    {
                        AnimateSelected();
                    }
                    else if (group.CurrentState.Name == "Unselected")
                    {
                        AnimateDeselected();
                    }
                };
            }
        }

        private void AnimateSelected()
        {
            AnimationExtensions.Scale(lineSeparator, 1, 2, 0, (float)lineSeparator.Height / 2, 300, 0, EasingType.Circle);
        }

        private void AnimateDeselected()
        {
            AnimationExtensions.Scale(lineSeparator, 1, (float).5, 0, (float)lineSeparator.Height / 2, 300, 0, EasingType.Circle);
        }

        private VisualStateGroup GetVisualStateGroup(string GroupName)
        {
            var groups = VisualStateManager.GetVisualStateGroups(this);
            foreach (var group in groups)
            {
                if (group.Name == GroupName)
                    return group;
            }
            return null;
        }
    }
}
