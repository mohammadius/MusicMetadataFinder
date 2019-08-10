using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MusicMetadataFinder.Extensions
{
    //get from http://stackoverflow.com/questions/1542274/how-do-i-combine-brushes-in-wpf
    class BrushAnimation : AnimationTimeline
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BrushAnimation();
        }

        public override Type TargetPropertyType => typeof(Brush);

        static BrushAnimation()
        {
            FromProperty = DependencyProperty.Register("From", typeof(Brush), typeof(BrushAnimation));
            ToProperty = DependencyProperty.Register("To", typeof(Brush), typeof(BrushAnimation));
        }

        public static readonly DependencyProperty FromProperty;
        public Brush From
        {
            get => (Brush)GetValue(BrushAnimation.FromProperty);
            set => SetValue(BrushAnimation.FromProperty, value);
        }

        public static readonly DependencyProperty ToProperty;
        public Brush To
        {
            get => (Brush)GetValue(BrushAnimation.ToProperty);
            set => SetValue(BrushAnimation.ToProperty, value);
        }

        public override object GetCurrentValue(object defaultOriginValue,
        object defaultDestinationValue, AnimationClock animationClock)
        {
            Brush fromVal = ((Brush)GetValue(BrushAnimation.FromProperty)).CloneCurrentValue();
            Brush toVal = ((Brush)GetValue(BrushAnimation.ToProperty)).CloneCurrentValue();

            if ((double)animationClock.CurrentProgress == 0.0)
            {
                return fromVal; //Here it workes fine.
            }

            if ((double)animationClock.CurrentProgress == 1.0)
            {
                return toVal; //It workes also here fine.
            }

            toVal.Opacity = (double)animationClock.CurrentProgress;


            Border Bd = new Border();
            Border Bdr = new Border();

            Bd.Width = 1.0;
            Bd.Height = 1.0;

            Bd.Background = fromVal;
            Bdr.Background = toVal;

            Bd.Visibility = Visibility.Visible;
            Bdr.Visibility = Visibility.Visible;
            Bd.Child = Bdr;

            Brush VB = new VisualBrush(Bd);
            return VB; //But here it return's a transparent brush.

            //If I return the 'toVal' variable here it animates correctly the opacity.
        }
    }
}