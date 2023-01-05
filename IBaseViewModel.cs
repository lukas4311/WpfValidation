namespace PCD.Core.Contracts
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Interface reprezentující základ view modelu.
    /// </summary>
    public interface IBaseViewModel : INotifyPropertyChanging, INotifyPropertyChanged
    {
        #region Methods

        /// <summary>
        /// Metoda pro nastavení property.
        /// </summary>
        /// <param name="value">Hodnota property.</param>
        /// <param name="propertyName">Název property. Doplní se automaticky.</param>
        /// <returns><c>True</c>, v případě kdy došlo k nastavení hodnoty, jinak <c>False</c>.</returns>
        bool SetValue(object value, [CallerMemberName] string propertyName = "");

        /// <summary>
        /// Metoda pro získání hodnoty property.
        /// </summary>
        /// <param name="propertyName">Název property.</param>
        /// <returns>Získaná hodnota.</returns>
        object GetValue([CallerMemberName] string propertyName = "");

        /// <summary>
        /// Metoda pro získání hodnoty property.
        /// </summary>
        /// <typeparam name="T">Typ hodnoty.</typeparam>
        /// <param name="propertyName">Název property.</param>
        /// <returns>Získaná hodnota.</returns>
        T GetValue<T>([CallerMemberName] string propertyName = "");

        /// <summary>
        /// Metoda pro vyvolání před změnou property.
        /// </summary>
        /// <param name="propertyName">Název property.</param>
        void OnPropertyChanging(string propertyName);

        /// <summary>
        /// Metoda pro vyvolání změny property.
        /// </summary>
        /// <param name="propertyName">Název property.</param>
        void OnPropertyChanged(string propertyName);

        #endregion
    }
}
