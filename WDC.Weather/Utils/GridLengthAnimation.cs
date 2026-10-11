using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace WDC.Weather.Utils
{
    public class GridLengthAnimation : AnimationTimeline
    {
        public static readonly DependencyProperty FromProperty
            = DependencyProperty.Register(nameof(From),
                                          typeof(GridLength),
                                          typeof(GridLengthAnimation));

        public GridLength From
        {
            get => (GridLength)this.GetValue(FromProperty);
            set => this.SetValue(FromProperty, value);
        }

        public static readonly DependencyProperty ToProperty
            = DependencyProperty.Register(nameof(To),
                                          typeof(GridLength),
                                          typeof(GridLengthAnimation));

        public GridLength To
        {
            get => (GridLength)this.GetValue(ToProperty);
            set => this.SetValue(ToProperty, value);
        }

        public IEasingFunction EasingFunction { get; set; }

        public override Type TargetPropertyType => typeof(GridLength);

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            var from = (GridLength)this.GetValue(FromProperty);
            var to = (GridLength)this.GetValue(ToProperty);
            if (from.GridUnitType != to.GridUnitType)
            {
                return to;
            }

            var fromVal = from.Value;
            var toVal = to.Value;

            if (fromVal > toVal)
            {
                return new GridLength((1 - animationClock.CurrentProgress.GetValueOrDefault()) * (fromVal - toVal) + toVal, GridUnitType.Star);
            }

            return new GridLength(animationClock.CurrentProgress.GetValueOrDefault() * (toVal - fromVal) + fromVal, GridUnitType.Star);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new GridLengthAnimation();
        }
    }
}
