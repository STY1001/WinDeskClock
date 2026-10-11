using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WinDeskClock.Views.Animation
{
    public enum SlideAxis { X, Y }

    /// <summary>
    /// Small, reusable wrappers around WPF's animation system (DoubleAnimation + BeginAnimation).
    ///
    /// This is what the original app was missing: every fade/scale/slide in the old code-behind
    /// hand-built its own DoubleAnimation + Storyboard.SetTarget/SetTargetProperty boilerplate
    /// (the same 6-15 lines, copy-pasted 500+ times), which is exactly what made the animation
    /// code fragile and hard to change consistently. Call sites now read as one line describing
    /// *what* moves, instead of *how* WPF's animation API works - same Storyboards under the hood,
    /// same visual result, far less code to get wrong.
    ///
    /// This class is intentionally View-only: it works directly with UIElement/FrameworkElement
    /// and WPF transforms, so it is only ever called from code-behind, never from a ViewModel.
    /// </summary>
    public static class UiAnimator
    {
        private static DoubleAnimation Build(double from, double to, double durationSeconds, IEasingFunction? easing, double beginTimeSeconds)
        {
            var animation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromSeconds(durationSeconds),
                EasingFunction = easing
            };
            if (beginTimeSeconds > 0)
            {
                animation.BeginTime = TimeSpan.FromSeconds(beginTimeSeconds);
            }
            return animation;
        }

        public static void Fade(UIElement element, double from, double to, double durationSeconds, IEasingFunction? easing = null, double beginTimeSeconds = 0)
        {
            element.BeginAnimation(UIElement.OpacityProperty, Build(from, to, durationSeconds, easing, beginTimeSeconds));
        }

        /// <summary>Animates both ScaleX and ScaleY together. Assumes element.RenderTransform is a ScaleTransform.</summary>
        public static void Scale(FrameworkElement element, double from, double to, double durationSeconds, IEasingFunction? easing = null, double beginTimeSeconds = 0)
        {
            // Fully qualified: UiAnimator declares a method called ScaleTransform (below), and a
            // method always hides a same-named type in plain-name lookup within its own class - so
            // the unqualified type name is not usable anywhere in this class, not just below.
            var transform = (ScaleTransform)element.RenderTransform;
            transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, Build(from, to, durationSeconds, easing, beginTimeSeconds));
            transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, Build(from, to, durationSeconds, easing, beginTimeSeconds));
        }

        /// <summary>Same as <see cref="Scale"/> but targeting an explicit ScaleTransform (e.g. one nested inside a TransformGroup).</summary>
        public static void ScaleTransform(ScaleTransform transform, double from, double to, double durationSeconds, IEasingFunction? easing = null)
        {
            transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, Build(from, to, durationSeconds, easing, 0));
            transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, Build(from, to, durationSeconds, easing, 0));
        }

        /// <summary>Animates a single axis of a ScaleTransform, leaving the other axis untouched - e.g.
        /// the CRT screen-off effect, which only ever compresses one dimension at a time (the bars
        /// close vertically while staying full-width, then the middle line closes horizontally).</summary>
        public static void ScaleAxis(ScaleTransform transform, SlideAxis axis, double from, double to, double durationSeconds, IEasingFunction? easing = null)
        {
            var property = axis == SlideAxis.X ? System.Windows.Media.ScaleTransform.ScaleXProperty : System.Windows.Media.ScaleTransform.ScaleYProperty;
            transform.BeginAnimation(property, Build(from, to, durationSeconds, easing, 0));
        }

        /// <summary>Animates a TranslateTransform on the given axis. Assumes element.RenderTransform is a TranslateTransform.</summary>
        public static void Translate(FrameworkElement element, SlideAxis axis, double from, double to, double durationSeconds, IEasingFunction? easing = null)
        {
            var transform = (TranslateTransform)element.RenderTransform;
            var property = axis == SlideAxis.X ? TranslateTransform.XProperty : TranslateTransform.YProperty;
            transform.BeginAnimation(property, Build(from, to, durationSeconds, easing, 0));
        }

        /// <summary>Same as <see cref="Translate"/> but targeting an explicit TranslateTransform.</summary>
        public static void TranslateOn(TranslateTransform transform, SlideAxis axis, double from, double to, double durationSeconds, IEasingFunction? easing = null)
        {
            var property = axis == SlideAxis.X ? TranslateTransform.XProperty : TranslateTransform.YProperty;
            transform.BeginAnimation(property, Build(from, to, durationSeconds, easing, 0));
        }

        public static void Width(FrameworkElement element, double from, double to, double durationSeconds, IEasingFunction? easing = null)
        {
            element.BeginAnimation(FrameworkElement.WidthProperty, Build(from, to, durationSeconds, easing, 0));
        }

        public static void Height(FrameworkElement element, double from, double to, double durationSeconds, IEasingFunction? easing = null)
        {
            element.BeginAnimation(FrameworkElement.HeightProperty, Build(from, to, durationSeconds, easing, 0));
        }

        /// <summary>Stops animating Height and lets the element size itself again (Height = Auto).</summary>
        public static void ReleaseHeight(FrameworkElement element)
        {
            element.BeginAnimation(FrameworkElement.HeightProperty, null);
            element.Height = double.NaN;
        }

        /// <summary>
        /// The "digit flip" pattern used by the clock, stopwatch, timer and alarm-card time editors:
        /// slide the current content out, swap it, slide the new content in from the opposite side.
        /// <paramref name="applyNewContent"/> is called at the midpoint (e.g. sets a TextBlock.Text
        /// or an Image.Source) - this keeps the helper usable for both text and bitmap digits.
        /// </summary>
        public static async Task SlideReplaceAsync(
            FrameworkElement element,
            SlideAxis axis,
            double outTo,
            double inFrom,
            Action applyNewContent,
            double outDurationSeconds,
            double inDurationSeconds,
            int midpointDelayMs,
            IEasingFunction? outEasing = null,
            IEasingFunction? inEasing = null)
        {
            Translate(element, axis, 0, outTo, outDurationSeconds, outEasing);
            await Task.Delay(midpointDelayMs);
            applyNewContent();
            Translate(element, axis, inFrom, 0, inDurationSeconds, inEasing);
        }
    }
}
