namespace PCD.Core.Contracts
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Třída reprezentující základ view modelu.
    /// </summary>
    /// <typeparam name="ParentVM">Typ view modelu.</typeparam>
    public abstract class BaseViewModel<ParentVM> : IBaseViewModel where ParentVM : IBaseViewModel
    {
        #region Constructors

        /// <summary>
        /// Inicializace nové instance třídy <see cref="BaseViewModel"/>.
        /// </summary>
        public BaseViewModel()
        {
        }

        #endregion

        #region Events

        /// <summary>
        /// Event handler spouštěný před změnou property.
        /// </summary>
        public event PropertyChangingEventHandler PropertyChanging;

        /// <summary>
        /// Event handler spouštěný po změně property.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Kolekce uchovávající hodnoty propert.
        /// </summary>
        private Dictionary<string, object> Values { get; set; } = new Dictionary<string, object>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Metoda pro získání hodnoty property.
        /// </summary>
        /// <param name="value">Typ hodnoty.</param>
        /// <param name="propertyName">Název property.</param>
        /// <returns><c>True</c>, v případě kdy došlo k nastavení hodnoty, jinak <c>False</c>.</returns>
        public virtual bool SetValue(object value, [CallerMemberName] string propertyName = "")
        {
            this.Values.TryGetValue(propertyName, out object actualValue);

            if (!object.Equals(actualValue, value))
            {
                this.OnPropertyChanging(propertyName);
                this.Values[propertyName] = value;
                this.OnPropertyChanged(propertyName);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Metoda pro získání hodnoty property.
        /// </summary>
        /// <param name="propertyName">Název property. Doplní se automaticky.</param>
        /// <returns>Hodnota property.</returns>
        public virtual object GetValue([CallerMemberName] string propertyName = "") => this.Values.ContainsKey(propertyName) ? this.Values[propertyName] : null;

        /// <summary>
        /// Metoda pro získání hodnoty property.
        /// </summary>
        /// <typeparam name="T">Typ hodnoty.</typeparam>
        /// <param name="propertyName">Název property.</param>
        /// <returns>Získaná hodnota.</returns>
        public virtual T GetValue<T>([CallerMemberName] string propertyName = "") => this.Values.ContainsKey(propertyName) ? (T)this.Values[propertyName] : default(T);

        /// <summary>
        /// Metoda pro vyvolání před změnou property.
        /// </summary>
        /// <param name="propertyName">Název property.</param>
        public virtual void OnPropertyChanging(string propertyName) => this.PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));

        /// <summary>
        /// Metoda pro vyvolání změny property.
        /// </summary>
        /// <param name="propertyName">Název property.</param>
        public virtual void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}
