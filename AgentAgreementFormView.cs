namespace PCD.Core.Modules.SalesNetwork.Agents.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using PCD.Core.Contracts;
    using PCD.Core.Contracts.ViewModel;
    using PCD.Core.Dtx;
    using PCD.WPFUtils.ValidationAttributes;
    using SystemInterface;

    /// <summary>
    /// View model pro Form
    /// </summary>
    public class AgentAgreementFormView : WpfAsyncValidationModel, IUserInfoMessage
    {
        #region Construktors

        /// <summary>
        /// Konstruktor pro model
        /// </summary>
        /// <param name="dataContext">Data context.</param>
        /// <param name="dateTime">Instance servici pro praci s casem.</param>        
        public AgentAgreementFormView(IDataContext dataContext, IDateTime dateTime)
        {
            this.DataContext = dataContext;
            this.DateTime = dateTime;

            this.ErrorsChanged += this.CheckAfterErrorsChanged;
            this.FirstDayofActualMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Data context.
        /// </summary>
        public IDataContext DataContext { get; private set; }

        /// <summary>
        /// Gets the date time.
        /// </summary>
        public IDateTime DateTime { get; private set; }

        /// <summary>
        /// Id agenta
        /// </summary>
        public int AgentId
        {
            get => (int)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Id parametru souhlasu
        /// </summary>
        public int? ParamId
        {
            get => (int?)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Souhlas agenta
        /// </summary>
        public AgentParam AgentParamAgreement
        {
            get => (AgentParam)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Souhlas agenta k ukončení
        /// </summary>
        private AgentParam AgentParamAgreementForClose
        {
            get => this.GetValue<AgentParam>();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Platnost souhlasu od
        /// </summary>
        [Required(ErrorMessage = "Zadejte datum")]
        [Condition(nameof(CheckValidFrom), ErrorMessage = "Platnost od nesmí být menší než platnost do")]
        [Condition(nameof(CheckDateValidFrom), ErrorMessage = "Platnost od nesmí být v minulém měsíci")]
        public DateTime? ValidFrom
        {
            get => (DateTime?)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Platnost souhlasu do
        /// </summary>
        //[Required(ErrorMessage = "Zadejte datum")]
        [Condition(nameof(CheckValidTo), ErrorMessage = "Platnost do nesmí být menší než platnost od")]
        [Condition(nameof(CheckDateValidTo), ErrorMessage = "Platnost do nesmí být v minulém měsíci")]
        public DateTime? ValidTo
        {
            get => (DateTime?)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Aktuální platnost do v db
        /// </summary>
        public DateTime? ActualValidTo
        {
            get => (DateTime?)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// První den v aktuálním měsíci
        /// </summary>
        public DateTime FirstDayofActualMonth
        {
            get => (DateTime)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Typy souhlasů agenta
        /// </summary>
        public List<TreeNode> AgentAgreementType
        {
            get => (List<TreeNode>)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Vybraný typ souhlasu agenta
        /// </summary>
        [Required(ErrorMessage = "Zadejte typ souhlasu")]
        public TreeNode SelectedAgentAgreementType
        {
            get => (TreeNode)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Příznak zda se jedná o nový záznam
        /// </summary>
        public bool IsNew
        {
            get => (bool)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// TreeNodeTypeId typů sohlasů
        /// </summary>
        public int TreeNodeTypeIdAgreementType
        {
            get => (int)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Priznak zda muze byt ulozeno
        /// </summary>
        public bool CanSave
        {
            get => (bool)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Příznak, zda je možná editace
        /// </summary>
        public bool CanEdit
        {
            get => (bool)this.GetValue();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Příznak, zda bylo uloženo
        /// </summary>
        public bool WasSaved
        {
            get => this.GetValue<bool>();
            set => this.SetValue(value);
        }

        /// <summary>
        /// Příznak zda je uživatel aktivní
        /// </summary>
        public bool UserIsEnabled
        {
            get => this.GetValue<bool>();
            set
            {
                this.SetValue(value);

                if (!value)
                {
                    this.CanSave = false;
                }
            }
        }

        /// <summary>
        /// Akce po ulozeni
        /// </summary>
        public Action SaveAction { get; set; }

        /// <summary>
        /// Akce po validaci
        /// </summary>
        public Action ValidationAction { get; set; }

        /// <summary>
        /// Kód typu informační zprávy.
        /// </summary>
        public UserInfoMessageCodes? UserInfoMessageCode { get => this.GetValue<UserInfoMessageCodes?>(); set => this.SetValue(value); }

        /// <summary>
        /// Informační zpráva.
        /// </summary>
        public string UserInfoMessage { get => this.GetValue<string>(); set => this.SetValue(value); }

        #endregion

        #region PublicMethods

        /// <summary>
        /// Metoda pro načtení
        /// </summary>
        /// <param name="agentId">Id agenta</param>
        /// <param name="paramId">Id parametru souhlasu</param>
        public void Load(int agentId, int? paramId)
        {
            this.AgentId = agentId;
            this.ParamId = paramId;
            this.CanSave = false;
            this.CanEdit = paramId == null;
            this.IsNew = paramId == null;
            Agent agent = Agent.Load(this.DataContext, this.AgentId);
            User user = User.Load(this.DataContext, agent.UserId);
            this.UserIsEnabled = user.IsEnabled;

            this.AgentAgreementType = TreeNode.GetByTypeCode(this.DataContext, TreeNodeTypes.AgentAgreementType);
            this.TreeNodeTypeIdAgreementType = TreeNode.GetIdByCode(this.DataContext as DataContext, AgentParamType.AgentAgreement);

            if (paramId.HasValue)
            {
                this.AgentParamAgreement = AgentParam.Load(this.DataContext, paramId.Value);
                this.SelectedAgentAgreementType = this.AgentAgreementType.Where(x => x.Id == (int)this.AgentParamAgreement.Value).FirstOrDefault();
                this.ValidFrom = this.AgentParamAgreement.ValidFrom;
                this.ValidTo = this.AgentParamAgreement.ValidTo;
                this.ActualValidTo = this.AgentParamAgreement.ValidTo;
            }
        }

        /// <summary>
        /// Metoda pro uložení
        /// </summary>
        public override void Save()
        {
            if (!this.CheckValidRecord())
            {
                this.ShowMessage($"Chyba překrývání platnosti u souhlasu {this.SelectedAgentAgreementType.Name}", false);
            }
            else
            {
                this.AgentParamAgreement = this.AgentParamAgreement ?? new AgentParam()
                {
                    AgentId = this.AgentId,
                    TreeNodeId = this.TreeNodeTypeIdAgreementType,
                    Code = this.SelectedAgentAgreementType.Code,
                    Name = this.SelectedAgentAgreementType.Name,
                    Description = this.SelectedAgentAgreementType.Description,
                    Value = this.SelectedAgentAgreementType.Id.Value
                };

                this.AgentParamAgreement.ValidFrom = this.ValidFrom.Value;
                this.AgentParamAgreement.ValidTo = this.ValidTo.Value;

                var saveAction = this.DataContext.RunInTransaction((Action)(() =>
                {
                    this.AgentParamAgreement.Save(this.DataContext);

                    if (this.AgentParamAgreementForClose != null)
                    {
                        this.AgentParamAgreementForClose.Save(this.DataContext);
                    }
                }));

                if (saveAction.success)
                {
                    this.WasSaved = true;
                    this.ParamId = this.AgentParamAgreement.Id;
                    this.CanEdit = false;
                    this.ShowMessage("Uložení proběhlo úpěšně", false);
                }
                else
                {
                    this.ShowMessage("Při uložení došlo k chybě:" + Environment.NewLine + saveAction.exceptinDetail.Message, true);
                }
            }
        }

        /// <summary>
        /// Metoda pro uložení view modelu.
        /// </summary>
        public void SaveAndClose()
        {
            this.Save();

            if (this.WasSaved)
            {
                this.SaveAction?.Invoke();
            }
        }

        /// <summary>
        /// Metoda pro změnu souhlasu
        /// </summary>
        public void ChangeAgreementType()
        {
            if (this.IsNew && this.SelectedAgentAgreementType.JsonMetadata != null)
            {
                JObject jsonData = JObject.Parse(this.SelectedAgentAgreementType.JsonMetadata);

                int? duration = (int?)jsonData["Duration"];
                string validityType = (string)jsonData["ValidityType"];
                this.ValidFrom = this.ValidFrom.HasValue ? this.ValidFrom : this.DateTime.Now.DateTimeInstance;

                if (duration.HasValue)
                {
                    if (validityType == "year")
                    {
                        this.ValidTo = this.ValidFrom.Value.AddYears(duration.Value);
                    }
                    else if (validityType == "month")
                    {
                        this.ValidTo = this.ValidFrom.Value.AddMonths(duration.Value);
                    }
                    else if (validityType == "day")
                    {
                        this.ValidTo = this.ValidFrom.Value.AddDays(duration.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Zobrazení informační hlášky
        /// </summary>
        /// <param name="mess">Zobrazená zpráva</param>
        /// <param name="error">Je to chyba</param>
        public void ShowMessage(string mess, bool error)
        {
            this.UserInfoMessageCode = error ? UserInfoMessageCodes.Error : UserInfoMessageCodes.Success;
            this.UserInfoMessage = mess;
            this.UserInfoMessageHideAfter();
        }

        #endregion

        #region PrivateMethods

        /// <summary>
        /// Kontrola zda model obsahuje invalidni sloupce
        /// </summary>
        /// <param name="sender">Odesilatel akce</param>
        /// <param name="e">Event akce</param>
        private void CheckAfterErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            this.CanSave = !this.HasErrors && this.UserIsEnabled;

            AppCache.Get<IApplicationService>()?.Invoke(() =>
            {
                this.ValidationAction?.Invoke();
            });
        }

        /// <summary>
        /// Validace data ukončení spolupráce
        /// </summary>
        /// <param name="value">Datum ukončení</param>
        /// <returns>Příznak validity</returns>
        private bool CheckValidFrom(object value)
        {
            bool isValid = true;

            if (value is DateTime validFrom)
            {
                isValid = !(this.ValidFrom > this.ValidTo);
            }

            return isValid;
        }

        /// <summary>
        /// Kontrola na hodnotu platnosti
        /// </summary>
        /// <param name="value">Zadaný datum</param>
        /// <returns>Příznak validity</returns>
        private bool CheckDateValidFrom(object value)
        {
            bool isValid = true;

            if (value is DateTime)
            {
                isValid = !(this.IsNew && this.ValidFrom < this.FirstDayofActualMonth);
            }

            return isValid;
        }

        /// <summary>
        /// Kontrola na hodnotu platnosti
        /// </summary>
        /// <param name="value">Zadaný datum</param>
        /// <returns>Příznak validity</returns>
        private bool CheckDateValidTo(object value)
        {
            bool isValid = true;

            if (value is DateTime)
            {
                isValid = this.ValidTo >= this.FirstDayofActualMonth;

                if (!this.IsNew && !isValid)
                {
                    isValid = this.ValidTo == this.ActualValidTo;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Validace data ukončení
        /// </summary>
        /// <param name="value">Datum ukončení</param>
        /// <returns>Příznak validity</returns>
        private bool CheckValidTo(object value)
        {
            bool isValid = false;

            if (value is DateTime validTo)
            {
                isValid = this.FirstDayofActualMonth <= this.ValidTo && this.ValidFrom <= this.ValidTo;
            }

            return isValid;
        }

        /// <summary>
        /// Kontrola na již existující souhlas
        /// </summary>
        /// <returns>Je záznam validní</returns>
        private bool CheckValidRecord()
        {
            bool isValid = true;

            if (!this.ParamId.HasValue)
            {
                List<AgentParam> actualAgentAgreements = AgentParam.Get(this.DataContext, new AgentParamFilter { AgentId = this.AgentId, TreeNodeIdCode = nameof(AgentParamType.AgentAgreement) }.ToFilters())
                    .Where(x => x.Code == this.SelectedAgentAgreementType.Code)
                    .ToList();

                if (actualAgentAgreements.Count > 0)
                {
                    if (actualAgentAgreements.Any(x => x.ValidFrom >= this.ValidFrom))
                    {
                        isValid = false;
                    }
                    else if (actualAgentAgreements.Any(x => x.ValidFrom <= this.ValidFrom && (x.ValidTo > this.ValidFrom || x.ValidTo == null)))
                    {
                        isValid = false;
                    }
                    else
                    {
                        AgentParam lastAgreement = actualAgentAgreements.OrderByDescending(x => x.ValidFrom).FirstOrDefault();

                        if (lastAgreement.ValidFrom < this.ValidFrom && (lastAgreement.ValidTo > this.ValidFrom || lastAgreement.ValidTo == null))
                        {
                            this.AgentParamAgreementForClose = lastAgreement;
                            this.AgentParamAgreementForClose.ValidTo = this.ValidFrom.Value.AddMinutes(-1);
                        }
                    }
                }
            }

            return isValid;
        }

        #endregion
    }
}