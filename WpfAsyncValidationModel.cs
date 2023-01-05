namespace PCD.Core.Contracts.ViewModel
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

    /// <summary>
    /// Třída reprezentující asynchronní validační WPF model.
    /// </summary>
    public class WpfAsyncValidationModel : WpfViewModel, INotifyDataErrorInfo
    {
        #region Variables

        /// <summary>
        /// Zámek.
        /// </summary>
        private object _lock = new object();

        /// <summary>
        /// The paused validatinos.
        /// </summary>
        private bool _doValidatinos = true;

        #endregion

        #region Constructors

        /// <summary>
        /// Inicializace nové instance třídy <see cref="WpfAsyncValidationModel"/>.
        /// </summary>
        public WpfAsyncValidationModel()
        {
            this.GetSummary();
        }

        #endregion

        #region Events

        /// <summary>
        /// Event handler pro zmenu s kolekci chyb.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        #endregion


        #region Properties

        /// <summary>
        /// Gets a value that indicates whether the entity has validation errors.
        /// </summary>
        public bool HasErrors => this.Errors.Any(e => e.Value?.Count > 0);

        /// <summary>
        /// Gets or sets the summary validation errors.
        /// </summary>
        public ConcurrentDictionary<(string propName, int order), string> SummaryValidationErrors { get; set; } = new ConcurrentDictionary<(string, int), string>();

        /// <summary>
        /// Ziskani summary message.
        /// </summary>
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
        protected ConcurrentDictionary<string, List<string>> Errors { get; set; } = new ConcurrentDictionary<string, List<string>>();

        /// <summary>
        /// Summary info pro properties.
        /// </summary>
        private List<PropertySummaryInfo> SummaryInfoAttributes { get; set; } = new List<PropertySummaryInfo>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Metoda pro nastavení property.
        /// </summary>
        /// <param name="value">Hodnota property.</param>
        /// <param name="propertyName">Název property. Doplní se automaticky.</param>
        public override void SetValue(object value, [CallerMemberName] string propertyName = "")
        {
            base.SetValue(value, propertyName);

            if (this._doValidatinos)
            {
                this.ValidateAsync();
            }
        }

        /// <summary>
        /// Metoda pro smazání view modelu.
        /// </summary>
        public override void Delete() => throw new NotImplementedException();

        /// <summary>
        /// Metoda pro získání popisu pole.
        /// </summary>
        /// <param name="property">Dependenci property daného pole.</param>
        /// <returns>Popis pole.</returns>
        public override string GetFieldDescription(string property) => throw new NotImplementedException();

        /// <summary>
        /// Metoda pro načtení view modelu.
        /// </summary>
        public override void Load() => throw new NotImplementedException();

        /// <summary>
        /// Metoda pro refresh view modelu nebo jen konkrétní property.
        /// </summary>
        /// <param name="property">Property pro refresh. Pokud není zadána, refresh celého view modelu.</param>
        public override void Refresh(string property = null) => throw new NotImplementedException();

        /// <summary>
        /// Metoda pro uložení view modelu.
        /// </summary>
        public override void Save() => throw new NotImplementedException();

        /// <summary>
        /// Volano pokud dojde ke zmene na kolekci chyb.
        /// </summary>
        /// <param name="propertyName">Nazev property.</param>
        public void OnErrorsChanged(string propertyName) => this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

        /// <summary>
        /// Gets the validation errors for a specified property or for the entire entity.
        /// </summary>
        /// <param name="propertyName">The name of the property to retrieve validation errors for; or <see langword="null" /> or <see cref="F:System.String.Empty" />, to retrieve entity-level errors.</param>
        /// <returns>
        /// The validation errors for the property or entity.
        /// </returns>
        public IEnumerable GetErrors(string propertyName)
        {
            this.Errors.TryGetValue(propertyName, out List<string> errorsForName);
            return errorsForName;
        }

        /// <summary>
        /// Zapauzovani validaci.
        /// </summary>
        public void PauseValidations() => this._doValidatinos = false;

        /// <summary>
        /// Spusteni validaci.
        /// </summary>
        public void ResumeValidations() => this._doValidatinos = true;

        /// <summary>
        /// Provedeni validace nad modelem.
        /// </summary>
        public void ValidateModel() => this.ValidateAsync();

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates the asynchronous.
        /// </summary>
        /// <returns>Task</returns>
        private Task ValidateAsync() => Task.Run(() => this.ValidateAllProperties());

        /// <summary>
        /// Validates all properties.
        /// </summary>
        private void ValidateAllProperties()
        {
            lock (this._lock)
            {
                ValidationContext validationContext = new ValidationContext(this, null, null);
                List<ValidationResult> validationResults = new List<ValidationResult>();
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

                List<IGrouping<string, ValidationResult>> errorsToChange = validationResultGroupedMemberNames.Where(s => !this.Errors.Any(e => e.Key == s.Key && s.Any(a => e.Value.Any(m => m == a.ErrorMessage)))).ToList();

                foreach (IGrouping<string, ValidationResult> groupingResult in errorsToChange)
                {
                    List<string> messages = groupingResult.Select(r => r.ErrorMessage).ToList();

                    this.Errors.TryRemove(groupingResult.Key, out List<string> _);
                    this.Errors.TryAdd(groupingResult.Key, messages);
                    this.AddSummaryInfo(groupingResult.Key, messages);

                    this.OnErrorsChanged(groupingResult.Key);
                }
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
    }

    internal class PropertySummaryInfo
    {
        public string PropertyName { get; set; }

        public int Order { get; set; }
    }
}