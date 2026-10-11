using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinDeskClock.Mvvm
{
    /// <summary>
    /// Base class for all ViewModels (and any other class that needs to notify the UI of property changes).
    /// Provides a small helper (<see cref="SetProperty{T}"/>) to reduce INotifyPropertyChanged boilerplate.
    /// </summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for the given property name.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the backing field if the new value is different, then raises PropertyChanged.
        /// Returns true if the value actually changed.
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Sets the backing field if the new value is different, raises PropertyChanged, and invokes a callback.
        /// Useful when a property change needs to trigger extra side effects (e.g. recomputing another property).
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (!SetProperty(ref field, value, propertyName))
            {
                return false;
            }

            onChanged();
            return true;
        }

        /// <summary>
        /// Raises PropertyChanged for several property names at once. Handy when one field backs
        /// several computed properties (e.g. a Days bitmask backing "IsOnce", "DaysSummary", etc.)
        /// </summary>
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                OnPropertyChanged(name);
            }
        }
    }
}
