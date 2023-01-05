namespace PCD.Core.Contracts
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Trida pro rozsireni tridy <see cref="Validator"/>
    /// </summary>
    public static class ValidatorExtension
    {
        #region Public Methods

        /// <summary>Metoda pro validaci view modelu</summary>
        /// <param name="propName">Nazev property</param>
        /// <param name="validationContext">Context popisujici hodnotu pro validaci</param>
        /// <param name="validationResults">Kolekce pro chyby z validaci</param>
        /// <returns>Vraci chybovou hlasku</returns>
        public static string WpfTryValidateProperty(string propName, ValidationContext validationContext, ICollection<ValidationResult> validationResults)
        {
            if (!(validationContext.ObjectInstance is WpfValidationModel validationModel))
            {
                throw new System.InvalidCastException("Metoda musí být volaná pro objekt, který dědí z objektu WpfValidationModel");
            }

            var value = validationContext.ObjectInstance.GetType().GetProperty(propName).GetValue(validationContext.ObjectInstance, null);
            var validationResult = Validator.TryValidateProperty(value, validationContext, validationResults);
            var retMsg = string.Empty;

            if (validationResult)
            {
                validationModel.InvalidProperties.Remove(validationContext.MemberName);
            }
            else
            {
                retMsg = validationResults.First().ErrorMessage;
                validationModel.InvalidProperties.AddIfNotContains(validationContext.MemberName);
            }

            return retMsg;
        }

        #endregion
    }
}
