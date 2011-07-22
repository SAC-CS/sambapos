﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Samba.Domain.Models.Tables;
using Samba.Infrastructure;
using Samba.Presentation.Common;
using Samba.Presentation.ViewModels;
using Samba.Services;

namespace Samba.Presentation.Terminal
{
    public static class DataContext
    {
        public static TicketViewModel SelectedTicket { get; private set; }

        private static TicketViewModel CreateSelectedTicket()
        {
            return new TicketViewModel(AppServices.MainDataContext.SelectedTicket, false);
        }

        public static TicketItemViewModel SelectedTicketItem { get; set; }

        public static void UpdateSelectedTicket(Table table)
        {
            Debug.Assert(SelectedTicket == null);
            if (table.TicketId == 0)
                AppServices.MainDataContext.AssignTableToSelectedTicket(table.Id);
            else AppServices.MainDataContext.OpenTicket(table.TicketId);
            SelectedTicket = CreateSelectedTicket();
        }

        public static void OpenTicket(int ticketId)
        {
            Debug.Assert(SelectedTicket == null);
            if (ticketId == 0) AppServices.MainDataContext.CreateNewTicket();
            else AppServices.MainDataContext.OpenTicket(ticketId);
            SelectedTicket = CreateSelectedTicket();
        }

        public static TicketCommitResult CloseSelectedTicket()
        {
            Debug.Assert(SelectedTicket != null);
            SelectedTicket = null;
            var result = AppServices.MainDataContext.CloseTicket();
            AppServices.MessagingService.SendMessage(Messages.TicketRefreshMessage, result.TicketId.ToString());
            return result;
        }

        internal static int MoveSelectedTicketItemsToNewTicket()
        {
            var result= AppServices.MainDataContext.MoveTicketItems(SelectedTicket.SelectedItems.Select(x => x.Model), 0).TicketId;
            SelectedTicket = null;
            return result;
        }
    }
}