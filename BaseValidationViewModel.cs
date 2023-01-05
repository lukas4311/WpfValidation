namespace PCD.Core.Contracts
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Serialization;

    /// <summary>
    /// Třída reprezentující základ view modelu s validací propert.
    /// </summary>
    /// <typeparam name="ParentVM">Typ view modelu.</typeparam>
    public abstract class BaseValidationViewModel<ParentVM> : BaseViewModel<ParentVM>, INotifyDataErrorInfo where ParentVM : IBaseViewModel
    {
        #region Variables

        /// <summary>
        /// Objekt reprezentující synchronizační zámek.
        /// </summary>
        private readonly object syncLock = new object();

        /// <summary>
        /// Příznak, zda probíhá validace.
        /// </summary>
        private bool isValidationInProgress;

        /// <summary>
        /// The paused validatinos
        /// </summary>
        private bool doValidatinos = true;

        #endregion

        #region Constructors

        /// <summary>
        /// Inicializace nové instance třídy <see cref="BaseValidationViewModel"/>.
        /// </summary>
        public BaseValidationViewModel() : base()
        {
            this.GetSummary();
        }

        #endregion

        #region Events

        /// <summary>
        /// Event pro změnu kolekce chyb validace.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Příznak indikující, zda model obsahuje chyby validace.
        /// </summary>
        [XmlIgnore]
        public virtual bool HasErrors => this.Errors.Any(e => e.Value?.Count > 0);

        /// <summary>
        /// Příznak, zda probíhá validace.
        /// </summary>
        [XmlIgnore]
        public bool IsValidationInProgress => this.isValidationInProgress;

        /// <summary>
        /// Gets or sets the summary validation errors.
        /// </summary>
        [XmlIgnore]
        public ConcurrentDictionary<(string propName, int order), string> SummaryValidationErrors { get; set; } = new ConcurrentDictionary<(string, int), string>();

        /// <summary>
        /// Ziskani summary message.
        /// </summary>
        [XmlIgnore]
        protected string SummaryInfoMessage
        {
            get
            {
                StringBuilder stringBuilder = new StringBuilder();
                this.SummaryValidationErrors.OrderBy(a => a.Key.order).ToList().ForEach(v => stringBuilder.AppendLine(v.Value));
                return stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Chyby validace.
        /// </summary>
        [XmlIgnore]
        protected ConcurrentDictionary<string, List<string>> Errors { get; set; } = new ConcurrentDictionary<string, List<string>>();

        /// <summary>
        /// Summary info pro properties.
        /// </summary>
        [XmlIgnore]
        private List<PropertySummaryInfo> SummaryInfoAttributes { get; set; } = new List<PropertySummaryInfo>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Metoda pro získání chyb validace dané property.
        /// </summary>
        /// <param name="propertyName">Název property.</param>
        /// <returns>Kolekce chyb validace property.</returns>
        public IEnumerable GetErrors(string propertyName)
        {
            var errorsForName = new List<string>();

            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                this.Errors.TryGetValue(propertyName, out errorsForName);
            }

            return errorsForName;
        }

        /// <summary>
        /// Metoda pro propagování změny chyby property při validaci.
        /// </summary>
        /// <param name="propertyName">Nazev property.</param>
        public void OnErrorsChanged(string propertyName) => this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

        /// <summary>
        /// Metoda pro získání hodnoty property.
        /// </summary>
        /// <param name="value">Typ hodnoty.</param>
        /// <param name="propertyName">Název property.</param>
        /// <returns><c>True</c>, v případě kdy došlo k nastavení hodnoty, jinak <c>False</c>.</returns>
        public override bool SetValue(object value, [CallerMemberName] string propertyName = "")
        {
            if (base.SetValue(value, propertyName))
            {
                if (this.doValidatinos)
                {
                    this.ValidateAsync();
                }

                return true;
            }

            return false;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Zapauzovani validaci.
        /// </summary>
        protected void PauseValidations() => this.doValidatinos = false;

        /// <summary>
        /// Spusteni validaci.
        /// </summary>
        protected void ResumeValidations() => this.doValidatinos = true;

        /// <summary>
        /// Provedeni validace nad modelem.
        /// </summary>
        protected void ValidateModel() => this.ValidateAsync();

        /// <summary>
        /// Metoda pro asynchroní validaci všech propert.
        /// </summary>
        /// <returns>Asynchronní task.</returns>
        protected Task ValidateAsync() => Task.Run(() => this.ValidateAllProperties());

        #endregion

        #region Private methods

        /// <summary>
        /// Metoda pro validaci všech propert.
        /// </summary>
        private void ValidateAllProperties()
        {
            lock (this.syncLock)
            {
                this.isValidationInProgress = true;
                this.OnPropertyChanged(nameof(this.IsValidationInProgress));
                var validationContext = new ValidationContext(this, null, null);
                var validationResults = new List<ValidationResult>();
                Validator.TryValidateObject(this, validationContext, validationResults, true);

                foreach (KeyValuePair<string, List<string>> errorMessagesForProperties in this.Errors.ToList())
                {
                    if (validationResults.All(r => r.MemberNames.All(m => m != errorMessagesForProperties.Key)))
                    {
                        this.Errors.TryRemove(errorMessagesForProperties.Key, out List<string> _);
                        this.RemoveSummaryInfo(errorMessagesForProperties.Key);
                        this.OnErrorsChanged(errorMessagesForProperties.Key);
                    }
                }

                IEnumerable<IGrouping<string, ValidationResult>> validationResultGroupedMemberNames = validationResults
                    .SelectMany(h => h.MemberNames, (validResult, memberNames) => new { validResult, memberNames })
                    .GroupBy(g => g.memberNames, e => e.validResult)
                    .Select(r => r);

                var errorsToChange = validationResultGroupedMemberNames.Where(s => !this.Errors.Any(e => e.Key == s.Key && s.Any(a => e.Value.Any(m => m == a.ErrorMessage)))).ToList();

                foreach (var groupingResult in errorsToChange)
                {
                    var messages = groupingResult.Select(r => r.ErrorMessage).ToList();
                    this.Errors.TryRemove(groupingResult.Key, out List<string> _);
                    this.Errors.TryAdd(groupingResult.Key, messages);
                    this.AddSummaryInfo(groupingResult.Key, messages);
                    this.OnErrorsChanged(groupingResult.Key);
                }

                this.isValidationInProgress = false;
                this.OnPropertyChanged(nameof(this.IsValidationInProgress));
            }
        }

        /// <summary>
        /// Pridani summary infa do seznamu.
        /// </summary>
        /// <param name="key">Klic kolekce.</param>
        /// <param name="message">Chybova zprava.</param>
        private void AddSummaryInfo(string key, List<string> message)
        {
            if (this.SummaryInfoAttributes.Any(a => a.PropertyName == key))
            {
                this.SummaryValidationErrors.TryRemove((key, this.GetOrderToProperty(key)), out string _);
                this.SummaryValidationErrors.TryAdd((key, this.GetOrderToProperty(key)), message.FirstOrDefault());
            }
        }

        /// <summary>
        /// Odebrani summary infa ze seznamu.
        /// </summary>
        /// <param name="key">Klic kolekce.</param>
        private void RemoveSummaryInfo(string key)
        {
            if (this.SummaryInfoAttributes.Any(a => a.PropertyName == key))
            {
                this.SummaryValidationErrors.TryRemove((key, this.GetOrderToProperty(key)), out string _);
            }
        }

        /// <summary>
        /// Zsikani poradi zpravy.
        /// </summary>
        /// <param name="key">Klic kolekce.</param>
        /// <returns>Poradi zpravy</returns>
        private int GetOrderToProperty(string key) => this.SummaryInfoAttributes.SingleOrDefault(a => a.PropertyName == key)?.Order ?? int.MaxValue;

        /// <summary>
        /// Ziskani seznamu summary info atributu na modelu.
        /// </summary>
        private void GetSummary()
        {
            foreach (PropertyInfo prop in this.GetType().GetProperties().Where(p => p.GetCustomAttributes(typeof(SummaryInfoAttribute), true).Any()))
            {
                foreach (SummaryInfoAttribute attr in prop.GetCustomAttributes(true).Where(a => a is SummaryInfoAttribute))
                {
                    this.SummaryInfoAttributes.Add(new PropertySummaryInfo { PropertyName = prop.Name, Order = attr.Order });
                }
            }
        }

        #endregion

        #region Private classes

        /// <summary>
        /// Třída reprezentující summary info.
        /// </summary>
        private class PropertySummaryInfo
        {
            #region Properties

            /// <summary>
            /// Název property.
            /// </summary>
            public string PropertyName { get; set; }

            /// <summary>
            /// Pořadí property.
            /// </summary>
            public int Order { get; set; }

            #endregion
        }

        #endregion
    }
}
