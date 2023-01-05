namespace PCD.Core.Modules.CRM
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using PCD.Core.Contracts;
    using PCD.Core.Dtx;
    using PCD.Core.Dtx.DataModels.Other.Crm;
    using PCD.Others.ApiDaktela;

    public class MysteryCallFormVM : BaseValidationViewModel<MysteryCallFormVM>, IPopupContentViewModel
    {
        private TreeNode mysteryAutoBatchTn;

        public MysteryCallFormVM()
        {
            this.MaxDayCountFromLastStateChange = null;
            this.MinDayCountFromLastStateChange = null;
            this.FirstStatusChangeFrom = null;
            this.FirstStatusChangeTo = null;

            using (IDataContext ctx = DataContext.NewByCache())
            {
                this.StatusList = new ObservableCollection<ContactStateModel>(ctx.Proc.Crm_ContactStateGet());
                this.MktSourceList = new ObservableCollection<MarketingSourceModel>(ctx.Proc.Crm_MarketingSourcesFullGet());
                this.AgentList = new ObservableCollection<AgentModel>(ctx.Proc.Crm_Agent_Get());
                this.mysteryAutoBatchTn = TreeNode.Get(ctx, TreeNodeTypes.DaktelaSettings, DaktelaSettings.MysteryAutoBatch);
                this.MysteryBatchEnabled = mysteryAutoBatchTn.IsEnabled;
            }

            this.SelectedContactState = this.StatusList.First();

            this.CreateBatchCommand = new PcdCommand(() =>
            {
                return Task.Run(() =>
                {
                    using (IDataContext ctx = DataContext.NewByCache())
                    {
                        ctx.Database.SqlCommandTimeout = 60;
                        DateTime firstStatusChangeTo = this.FirstStatusChangeTo ?? DateTime.MinValue;
                        DateTime firstStatusChangeFrom = this.FirstStatusChangeFrom ?? DateTime.MinValue;
                        int statusId = this.SelectedContactState.Id;
                        int mktSource = this.SelectedMarketingSource?.Id ?? 00;
                        int agentId = int.Parse(this.SelectedAgent?.Id.ToString() ?? "0");
                        int maxDayCountFromLastStateChange = this.MaxDayCountFromLastStateChange ?? 0;
                        int minDayCountFromLastStateChange = this.MinDayCountFromLastStateChange ?? 0;
                        int batchType = this.BatchType + 1;

                        List<DaktelaMysteryCallModel> daktelaResults = ctx.Proc.Crm_GetDaktelaMysteryCall(batchType, firstStatusChangeFrom, firstStatusChangeTo, statusId, this.RegionId, agentId, mktSource, minDayCountFromLastStateChange, maxDayCountFromLastStateChange).ToList();

                        int ret = 0;

                        if (daktelaResults.Any())
                        {
                            IDaktela daktela = DaktelaFactory.Get(6);

                            foreach (DaktelaMysteryCallModel dr in daktelaResults)
                            {
                                daktela.ImportCallRecord(dr.QueueId, dr.PhoneNo, out string recordId, dr.GetFileds());
                                MysteryContactModel mysteryContactModel = new MysteryContactModel
                                {
                                    AgentCode = dr.AgentCode,
                                    BatchType = batchType,
                                    ContactId = dr.IdContact,
                                    CreatedDate = DateTime.Now,
                                    DateFrom = firstStatusChangeFrom,
                                    DateTo = firstStatusChangeTo,
                                    StatusId = statusId
                                };

                                ctx.Proc.Crm_MysteryContactSave(mysteryContactModel);
                                ret++;
                            }
                        }

                        this.ResultBrush = ret == 0 ? "#FFFF0000" : "#FF008000";
                        this.Result = ret == 0 ? "Dávka nebyla vygenerována, zadaným podmínkám nevyhovuje žádný záznam." : $"Dávka byla vygenerována, obsahuje: {ret} záznamů.";
                    }
                });
            }, () => !this.HasErrors)
            {
                WaitMessage = "Vytváření dávky..."
            };

            this.CreateBatchLastDayCommand = new PcdCommand(() =>
            {
                return Task.Run(() =>
                {
                    using (IDataContext ctx = DataContext.NewByCache())
                    {
                        ctx.Database.SqlCommandTimeout = 60;
                        DateTime firstStatusChangeTo = DateTime.Now.AddHours(-2);
                        DateTime firstStatusChangeFrom = DateTime.Now.AddDays(-1);
                        int statusId = 9;
                        int mktSource = 0;
                        int agentId = 0;
                        int maxDayCountFromLastStateChange = 0;
                        int minDayCountFromLastStateChange = 0;
                        int batchType = 1;

                        List<DaktelaMysteryCallModel> daktelaResults = ctx.Proc.Crm_GetDaktelaMysteryCall(batchType, firstStatusChangeFrom, firstStatusChangeTo, statusId, this.RegionId, agentId, mktSource, minDayCountFromLastStateChange, maxDayCountFromLastStateChange).ToList();

                        int ret = 0;

                        if (daktelaResults.Any())
                        {
                            IDaktela daktela = DaktelaFactory.Get(6);

                            foreach (DaktelaMysteryCallModel dr in daktelaResults)
                            {
                                daktela.ImportCallRecord(dr.QueueId, dr.PhoneNo, out string recordId, dr.GetFileds());
                                MysteryContactModel mysteryContactModel = new MysteryContactModel
                                {
                                    AgentCode = dr.AgentCode,
                                    BatchType = batchType,
                                    ContactId = dr.IdContact,
                                    CreatedDate = DateTime.Now,
                                    DateFrom = firstStatusChangeFrom,
                                    DateTo = firstStatusChangeTo,
                                    StatusId = statusId
                                };

                                ctx.Proc.Crm_MysteryContactSave(mysteryContactModel);
                                ret++;
                            }
                        }

                        this.ResultBrush = ret == 0 ? "#FFFF0000" : "#FF008000";
                        this.Result = ret == 0 ? "Dávka nebyla vygenerována, zadaným podmínkám nevyhovuje žádný záznam." : $"Dávka byla vygenerována, obsahuje: {ret} záznamů.";
                    }
                });
            }, () => true)
            {
                WaitMessage = "Vytváření dávky..."
            };
        }

        #region Properties
        /// <summary>
        /// Data pro combo status
        /// </summary>
        public ObservableCollection<ContactStateModel> StatusList
        {
            get { return this.GetValue<ObservableCollection<ContactStateModel>>(); }
            set { this.SetValue(value); }
        }

        /// <summary>
        /// Vybraný stav kontaktu
        /// </summary>
        [Required]
        public ContactStateModel SelectedContactState
        {
            get { return this.GetValue<ContactStateModel>(); }
            set { this.SetValue(value); }
        }

        /// <summary>
        /// Data pro combo MKT zdroje
        /// </summary>
        public ObservableCollection<MarketingSourceModel> MktSourceList
        {
            get { return this.GetValue<ObservableCollection<MarketingSourceModel>>(); }
            set { this.SetValue(value); }
        }

        /// <summary>
        /// Vybraný MKT zdroj
        /// </summary>
        public MarketingSourceModel SelectedMarketingSource
        {
            get { return this.GetValue<MarketingSourceModel>(); }
            set { this.SetValue(value); }
        }

        /// <summary>
        /// Data pro combo ÚP
        /// </summary>
        public ObservableCollection<AgentModel> AgentList
        {
            get { return this.GetValue<ObservableCollection<AgentModel>>(); }
            set { this.SetValue(value); }
        }

        public AgentModel SelectedAgent
        {
            get { return this.GetValue<AgentModel>(); }
            set { this.SetValue(value); }
        }

        public PcdCommand CreateBatchCommand { get; private set; }

        public PcdCommand CreateBatchLastDayCommand { get; private set; }

        public int BatchType
        {
            get { return this.GetValue<int>(); }
            set { this.SetValue(value); }
        }

        public int RegionId
        {
            get { return this.GetValue<int>(); }
            set { this.SetValue(value); }
        }

        public int? MinDayCountFromLastStateChange
        {
            get { return this.GetValue<int?>(); }
            set { this.SetValue(value); }
        }

        public int? MaxDayCountFromLastStateChange
        {
            get { return this.GetValue<int?>(); }
            set { this.SetValue(value); }
        }

        [Required]
        public DateTime? FirstStatusChangeFrom
        {
            get { return this.GetValue<DateTime?>(); }
            set { this.SetValue(value); }
        }

        [Required]
        public DateTime? FirstStatusChangeTo
        {
            get { return this.GetValue<DateTime?>(); }
            set { this.SetValue(value); }
        }

        public string Result
        {
            get { return this.GetValue<string>(); }
            set { this.SetValue(value); }
        }

        public bool ResultVisible
        {
            get { return this.GetValue<bool>(); }
            set { this.SetValue(value); }
        }

        public bool MysteryBatchEnabled
        {
            get { return this.GetValue<bool>(); }
            set { this.SetValue(value); }
        }

        public string ResultBrush { get => this.GetValue<string>(); set => this.SetValue(value); }

        public List<IPcdCommand> PopupCommands { get; set; }

        public IPcdCommand PopupClosedCommand { get; set; }

        public string PopupCaption { get; set; }
        #endregion

        public override void OnPropertyChanged(string propertyName)
        {
            if (propertyName == nameof(this.Result))
            {
                this.ResultVisible = !string.IsNullOrEmpty(this.Result);
            }
            if (propertyName == nameof(this.MysteryBatchEnabled))
            {
                using DataContext ctx = new DataContext(AppCache.Get<ApplicationContext>(AppCacheBaseKeys.ApplicationContext).ConnectionString, AppCache.Get<IUserContext>(AppCacheBaseKeys.UserContext).GetCallContext());
                TreeNode.ChangeEnabled(ctx, TreeNodeTypes.DaktelaSettings, DaktelaSettings.MysteryAutoBatch, MysteryBatchEnabled);
            }
            base.OnPropertyChanged(propertyName);
        }
    }
}
