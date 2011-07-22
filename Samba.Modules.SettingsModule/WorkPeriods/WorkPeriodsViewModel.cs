﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Services;
using System.Linq;

namespace Samba.Modules.SettingsModule.WorkPeriods
{
    [Export]
    public class WorkPeriodsViewModel : ObservableObject
    {
        private IEnumerable<WorkPeriodViewModel> _workPeriods;
        public IEnumerable<WorkPeriodViewModel> WorkPeriods
        {
            get
            {
                return _workPeriods ?? (_workPeriods = Dao.Last<WorkPeriod>(30)
                                                           .OrderByDescending(x => x.Id)
                                                           .Select(x => new WorkPeriodViewModel(x)));
            }
            
        }

        public ICaptionCommand StartOfDayCommand { get; set; }
        public ICaptionCommand EndOfDayCommand { get; set; }
        public ICaptionCommand DisplayStartOfDayScreenCommand { get; set; }
        public ICaptionCommand DisplayEndOfDayScreenCommand { get; set; }
        public ICaptionCommand CancelCommand { get; set; }

        public WorkPeriod LastWorkPeriod { get { return AppServices.MainDataContext.CurrentWorkPeriod; } }

        public TimeSpan WorkPeriodTime { get; set; }

        private int _openTicketCount;
        public int OpenTicketCount
        {
            get { return _openTicketCount; }
            set { _openTicketCount = value; RaisePropertyChanged("OpenTicketCount"); }
        }

        private string _openTicketLabel;
        public string OpenTicketLabel
        {
            get { return _openTicketLabel; }
            set { _openTicketLabel = value; RaisePropertyChanged("OpenTicketLabel"); }
        }

        private int _activeScreen;
        public int ActiveScreen
        {
            get { return _activeScreen; }
            set { _activeScreen = value; RaisePropertyChanged("ActiveScreen"); }
        }

        public string StartDescription { get; set; }
        public string EndDescription { get; set; }
        public decimal CashAmount { get; set; }
        public decimal CreditCardAmount { get; set; }
        public decimal TicketAmount { get; set; }

        public string LastEndOfDayLabel
        {
            get
            {
                if (AppServices.MainDataContext.IsCurrentWorkPeriodOpen)
                {
                    var title1 = string.Format("Gün başlama tarihi: {0}", LastWorkPeriod.StartDate.ToShortDateString());
                    var title2 = string.Format("Gün başlama saati: {0}", LastWorkPeriod.StartDate.ToShortTimeString());
                    var title3 = string.Format("Toplam Çalışma Zamanı:\r{0:0} gün {1:0} saat {2:0} dakika", WorkPeriodTime.Days, WorkPeriodTime.Hours, WorkPeriodTime.Minutes);
                    if (WorkPeriodTime.Days == 0) title3 = string.Format("Toplam Çalışma Zamanı: {0:0} saat {1:0} dakika", WorkPeriodTime.Hours, WorkPeriodTime.Minutes);
                    if (WorkPeriodTime.Hours == 0) title3 = string.Format("Toplam Çalışma Zamanı: {0:0} dakika", WorkPeriodTime.TotalMinutes);
                    return title1 + "\r\n" + title2 + "\r\n" + title3;
                }
                return "Çalışmaya başlamak için gün başı yapınız.";
            }
        }

        public WorkPeriodsViewModel()
        {
            StartOfDayCommand = new CaptionCommand<string>("Gün Başı Yap", OnStartOfDayExecute, CanStartOfDayExecute);
            EndOfDayCommand = new CaptionCommand<string>("Gün Sonu Yap", OnEndOfDayExecute, CanEndOfDayExecute);
            DisplayStartOfDayScreenCommand = new CaptionCommand<string>("Gün Başı", OnDisplayStartOfDayScreenCommand, CanStartOfDayExecute);
            DisplayEndOfDayScreenCommand = new CaptionCommand<string>("Gün Sonu", OnDisplayEndOfDayScreenCommand, CanEndOfDayExecute);
            CancelCommand = new CaptionCommand<string>("İptal", OnCancel);
        }

        private void OnCancel(string obj)
        {
            ActiveScreen = 0;
        }

        private void OnDisplayEndOfDayScreenCommand(string obj)
        {
            ActiveScreen = 2;
        }

        private void OnDisplayStartOfDayScreenCommand(string obj)
        {
            ActiveScreen = 1;
        }

        private bool CanEndOfDayExecute(string arg)
        {
            return AppServices.ActiveAppScreen == AppScreens.WorkPeriods
                && OpenTicketCount == 0
                && WorkPeriodTime.TotalMinutes > 1
                && AppServices.MainDataContext.IsCurrentWorkPeriodOpen;
        }

        private void OnEndOfDayExecute(string obj)
        {
            AppServices.MainDataContext.StopWorkPeriod(EndDescription);
            Refresh();
            AppServices.MainDataContext.CurrentWorkPeriod.PublishEvent(EventTopicNames.WorkPeriodStatusChanged);
            InteractionService.UserIntraction.GiveFeedback("Gün Sonu İşlemi Tamamlandı.\rRaporlar bölümünden gün sonu raporlarınızı alınız.");
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateNavigation);
        }

        private bool CanStartOfDayExecute(string arg)
        {
            return AppServices.ActiveAppScreen == AppScreens.WorkPeriods
                && (LastWorkPeriod == null || LastWorkPeriod.StartDate != LastWorkPeriod.EndDate);
        }

        private void OnStartOfDayExecute(string obj)
        {
            AppServices.MainDataContext.StartWorkPeriod(StartDescription, CashAmount, CreditCardAmount, TicketAmount);
            Refresh();
            AppServices.MainDataContext.CurrentWorkPeriod.PublishEvent(EventTopicNames.WorkPeriodStatusChanged);
            InteractionService.UserIntraction.GiveFeedback("Gün Başı İşlemi Tamamlandı.");
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateNavigation);
        }

        public void Refresh()
        {
            _workPeriods = null;
            if (LastWorkPeriod != null)
                WorkPeriodTime = new TimeSpan(DateTime.Now.Ticks - LastWorkPeriod.StartDate.Ticks);
            OpenTicketCount = Dao.Count<Ticket>(x => !x.IsPaid);

            if (OpenTicketCount > 0)
            {
                OpenTicketLabel = string.Format("Açık {0} adisyon bulunmaktadır.\r\nAçık adisyon varken gün sonu yapılamaz.",
                                  OpenTicketCount);
            }
            else OpenTicketLabel = "";

            RaisePropertyChanged("WorkPeriods");
            RaisePropertyChanged("LastEndOfDayLabel");
            RaisePropertyChanged("WorkPeriods");

            StartDescription = "";
            EndDescription = "";
            CashAmount = 0;
            CreditCardAmount = 0;
            TicketAmount = 0;

            ActiveScreen = 0;
        }
    }
}