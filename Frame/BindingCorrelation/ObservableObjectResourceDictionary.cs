using CommunityToolkit.Mvvm.ComponentModel;
using Machine.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WPFMachine.Frame.BindingCorrelation
{
    public class ObservableObjectResourceDictionary : ObservableObject
    {
        /// <summary>
        /// Compares the current and new values for a given property. If the value has changed,
        /// raises the <see cref="PropertyChanging"/> event, updates the property with the new
        /// value, then raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <typeparam name="T">The type of the property that changed.</typeparam>
        /// <param name="field">The field storing the property's value.</param>
        /// <param name="newValue">The property's value after the change occurred.</param>
        /// <param name="propertyName">(optional) The name of the property that changed.</param>
        /// <returns><see langword="true"/> if the property was changed, <see langword="false"/> otherwise.</returns>
        /// <remarks>
        /// The <see cref="PropertyChanging"/> and <see cref="PropertyChanged"/> events are not raised
        /// if the current and new value for the target property are the same.
        /// </remarks>
        protected bool SetProperty<T>([NotNullIfNotNull(nameof(newValue))] ref T field, T newValue, Action CallBack, [CallerMemberName] string? propertyName = null)
        {
            // We duplicate the code here instead of calling the overload because we can't
            // guarantee that the invoked SetProperty<T> will be inlined, and we need the JIT
            // to be able to see the full EqualityComparer<T>.Default.Equals call, so that
            // it'll use the intrinsics version of it and just replace the whole invocation
            // with a direct comparison when possible (eg. for primitive numeric types).
            // This is the fastest SetProperty<T> overload so we particularly care about
            // the codegen quality here, and the code is small and simple enough so that
            // duplicating it still doesn't make the whole class harder to maintain.
            if (EqualityComparer<T>.Default.Equals(field, newValue))
            {
                return false;
            }

            OnPropertyChanging(propertyName);

            field = newValue;
            CallBack();

            OnPropertyChanged(propertyName);

            return true;
        }

        protected virtual bool SetPropertyResourceDictionary<T>(ref T storage, string value, [CallerMemberName] string propertyName = null)
        {
            var temp = Application.Current.Resources[value];
            if (temp is not T) return false;
            var Language = (T)temp;
            if (EqualityComparer<T>.Default.Equals(storage, Language)) return false;
            OnPropertyChanging(propertyName);
            storage = Language;
            OnPropertyChanged(propertyName);
            return true;
        }

    }
}
