﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Users;
using Samba.Infrastructure.Data;
using Samba.Infrastructure.Settings;
using Samba.Persistance.Data;

namespace Samba.Services
{
    public enum AppScreens
    {
        LoginScreen,
        Navigation,
        SingleTicket,
        TicketList,
        Payment,
        TableList,
        CustomerList,
        WorkPeriods,
        Dashboard,
        CashView
    }

    public static class AppServices
    {
        public static Dispatcher MainDispatcher { get; set; }
        public static IWorkspace Workspace;
        public static MainDataContext MainDataContext { get; set; }
        public static AppScreens ActiveAppScreen { get; set; }

        private static PrinterService _printService;
        public static PrinterService PrintService
        {
            get { return _printService ?? (_printService = new PrinterService()); }
        }

        private static DataAccessService _dataAccessService;
        public static DataAccessService DataAccessService
        {
            get { return _dataAccessService ?? (_dataAccessService = new DataAccessService()); }
        }

        private static MessagingService _messagingService;
        public static MessagingService MessagingService
        {
            get { return _messagingService ?? (_messagingService = new MessagingService()); }
        }

        private static CashService _cashService;
        public static CashService CashService
        {
            get { return _cashService ?? (_cashService = new CashService()); }
        }

        private static SettingService _settingService;
        public static SettingService SettingService
        {
            get { return _settingService ?? (_settingService = new SettingService()); }
        }

        static AppServices()
        {
            CurrentLoggedInUser = User.Nobody;
            Workspace = WorkspaceFactory.Create();
            MainDataContext = new MainDataContext();
        }

        private static Terminal _terminal;
        public static Terminal CurrentTerminal { get { return _terminal ?? (_terminal = GetCurrentTerminal()); } set { _terminal = value; } }
        public static User CurrentLoggedInUser { get; private set; }

        public static bool CanNavigate()
        {
            return MainDataContext.SelectedTicket == null;
        }

        public static bool CanStartApplication()
        {
            return LocalSettings.CurrentDbVersion <= 0 || LocalSettings.CurrentDbVersion == LocalSettings.DbVersion;
        }

        public static bool CanModifyTicket()
        {
            return true;
        }

        private static User GetUserByPinCode(string pinCode)
        {
            return Workspace.All<User>(x => x.PinCode == pinCode).FirstOrDefault();
        }

        private static LoginStatus CheckPinCodeStatus(string pinCode)
        {
            var users = Workspace.All<User>(x => x.PinCode == pinCode);
            return users.Count() == 0 ? LoginStatus.PinNotFound : LoginStatus.CanLogin;
        }

        private static Terminal GetCurrentTerminal()
        {
            if (!string.IsNullOrEmpty(LocalSettings.TerminalName))
            {
                var terminal = Workspace.Single<Terminal>(x => x.Name == LocalSettings.TerminalName);
                if (terminal != null) return terminal;
            }
            var dterminal = Workspace.Single<Terminal>(x => x.IsDefault);
            return dterminal ?? Terminal.DefaultTerminal;
        }

        public static void LogError(Exception e)
        {
            MessageBox.Show("Bir sorun tespit ettik.\r\n\r\nProgram çalışmaya devam edecek ancak en kısa zamanda teknik destek almanız önerilir. Lütfen teknik destek için program danışmanınız ile irtibat kurunuz.\r\n\r\nMesaj:\r\n" + e.Message, "Bilgi", MessageBoxButton.OK, MessageBoxImage.Stop);
            Logger.Write(e, "General");
        }

        public static void LogError(Exception e, string userMessage)
        {
            MessageBox.Show(userMessage, "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            Logger.Write(e, "General");
        }

        public static void Log(string message)
        {
            Logger.Write(message, "User");
        }

        public static void Log(string message, string category)
        {
            Logger.Write(message, category);
        }

        public static void ResetCache()
        {
            _terminal = null;
            MainDataContext.ResetCache();
            PrintService.ResetCache();
            SerialPortService.ResetCache();
            Dao.ResetCache();
            Workspace = WorkspaceFactory.Create();
        }

        public static User LoginUser(string pinValue)
        {
            Debug.Assert(CurrentLoggedInUser == User.Nobody);
            CurrentLoggedInUser = CanStartApplication() && CheckPinCodeStatus(pinValue) == LoginStatus.CanLogin ? GetUserByPinCode(pinValue) : User.Nobody;
            MainDataContext.ResetUserData();
            return CurrentLoggedInUser;
        }

        public static void LogoutUser()
        {
            Debug.Assert(CurrentLoggedInUser != User.Nobody);
            CurrentLoggedInUser = User.Nobody;
            ResetCache();
        }

        public static bool IsUserPermittedFor(string p)
        {
            if (CurrentLoggedInUser.UserRole.IsAdmin) return true;
            if (CurrentLoggedInUser.UserRole.Id == 0) return false;
            var permission = CurrentLoggedInUser.UserRole.Permissions.SingleOrDefault(x => x.Name == p);
            if (permission == null) return false;
            return permission.Value == (int)PermissionValue.Enabled;
        }
    }
}