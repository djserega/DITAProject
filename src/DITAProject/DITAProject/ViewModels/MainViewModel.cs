using Atlassian.Jira;
using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ITAJira.ViewModels
{
    internal class MainViewModel : BindableBase, ISingleton
    {
        public Models.JiraModel.Сonnector Connector { get; set; }
        public Models.JiraModel.Сonnector ConnectorTime { get; set; }

        public MainViewModel()
        {
            _textToFilterListTasks = string.Empty;

            Connector = new();
            ConnectorTime = new();

            InitEvents();
        }

        private void InitEvents()
        {
            Models.JiraModel.Сonnector.ListStatusUpdatingEvent += (object? sender, List<Models.JiraModel.TaskStatus> e) => ListStatuses = new(e);
            Models.JiraModel.Сonnector.ListTaskUpdatingEvent += (object? sender, List<Models.JiraModel.Task> e) =>
            {
                ListTasks = new(e);
                ListTasksView = CollectionViewSource.GetDefaultView(ListTasks);

                System.Windows.Application.Current.Dispatcher.Invoke(() => { ShowCloseReportPage(); });
            };
            Models.JiraModel.Сonnector.ListUsersInListTask += (object? sender, List<Models.JiraModel.User> users) =>
            {
                FilterUsers = new(users.ConvertAll(new Converter<Models.JiraModel.User, Models.FilterUser>(ConverterJiraUserToFilterUser)));
            };

            Models.JiraModel.TaskStatus.ChangeCheckMarkStatusEvent += (object? sender, string nameStatus) =>
            {
                ChangeIsCheckedStatusByName(nameStatus);
            };
            Models.FilterUser.ChangeCheckMarkUserEvent += (object? sender, string userName) =>
            {
                ChangeIsCheckedUserByName(userName);
            };
        }

        private static Models.FilterUser ConverterJiraUserToFilterUser(Models.JiraModel.User user)
            => new(user);

        public DateTime DateTimeStartPeriod { get; set; } = DateTime.Now.AddDays(-7);
        public DateTime DateTimeEndPeriod { get; set; } = DateTime.Now;

        #region Statuses

        public Models.JiraModel.TaskStatus? SelectedStatus { get; set; }

        public ObservableCollection<Models.JiraModel.TaskStatus> ListStatuses { get; set; } = new();

        public ICommand CheckAllStatusesCommand => new DelegateCommand(() => ChangeIsCheckedStatuses(true));
        public ICommand UnCheckAllStatusesCommand => new DelegateCommand(() => ChangeIsCheckedStatuses(false));
        public ICommand ChangeIsCheckedStatusCommand => new DelegateCommand(() => ChangeIsCheckedStatusByName(SelectedStatus?.Name));


        private void ChangeIsCheckedStatusByName(string? nameStatus)
        {
            if (string.IsNullOrEmpty(nameStatus))
                return;

            if (ListStatuses.Any(el => el.Name == nameStatus))
                ListStatuses.First(el => el.Name == nameStatus).IsChecked = !ListStatuses.First(el => el.Name == nameStatus).IsChecked;

            InitFilterListTasks();
        }

        private void ChangeIsCheckedStatuses(bool newStatus)
        {
            for (int i = 0; i < ListStatuses.Count; i++)
                ListStatuses[i].IsChecked = newStatus;

            InitFilterListTasks();
        }

        #endregion

        #region Users

        public Models.FilterUser? SelectedFilterUser { get; set; }

        public ObservableCollection<Models.FilterUser> FilterUsers { get; set; } = new();

        public ICommand CheckAllUsersCommand => new DelegateCommand(() => ChangeIsCheckedUsers(true));
        public ICommand UnCheckAllUsersCommand => new DelegateCommand(() => ChangeIsCheckedUsers(false));
        public ICommand ChangeIsCheckedUserCommand => new DelegateCommand(() => ChangeIsCheckedUserByName(SelectedFilterUser?.User?.Name));


        private void ChangeIsCheckedUserByName(string? userName)
        {
            if (string.IsNullOrEmpty(userName))
                return;

            if (FilterUsers.Any(el => el.User?.Name == userName))
                FilterUsers.First(el => el.User?.Name == userName).IsChecked = !FilterUsers.First(el => el.User?.Name == userName).IsChecked;

            InitFilterListTasks();
        }

        private void ChangeIsCheckedUsers(bool newStatus)
        {
            for (int i = 0; i < FilterUsers.Count; i++)
                FilterUsers[i].IsChecked = newStatus;

            InitFilterListTasks();
        }

        #endregion

        #region Tasks

        public bool UpdatingTimeLog { get; set; }

        private Models.JiraModel.Task? _selectedTask;
        public Models.JiraModel.Task? SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;

                if (ShowTimeSpentDetailed)
                {
                    Task.Run(() =>
                    {
                        UpdatingTimeLog = true;

                        IEnumerable<IssueChangeLog>? changeLogs = ConnectorTime.GetTimeSpentFromIssue(_selectedTask?.Key);
                        if (_selectedTask != null && changeLogs != null)
                            _selectedTask.FillTimeSpent(changeLogs);

                        UpdatingTimeLog = false;
                    });
                }
            }
        }

        public List<Models.JiraModel.User> Users { get; set; } = new();

        private string _textToFilterListTasks;
        public string TextToFilterListTasks
        {
            get => _textToFilterListTasks;
            set
            {
                _textToFilterListTasks = value;
                InitFilterListTasks();
            }
        }

        public ICollectionView? ListTasksView { get; private set; }
        public ObservableCollection<Models.JiraModel.Task> ListTasks { get; set; } = new();

        public ICommand OpenSelectedTaskCommand => new DelegateCommand(() =>
        {
            if (SelectedTask == null)
                return;

            string prefix = Connector.Address;
            if (prefix.Last() != '/')
                prefix += "/";

            Process.Start(new ProcessStartInfo(prefix + "browse/" + SelectedTask.Key)
            {
                UseShellExecute = true,
                Verb = "open"
            });
        }, () => SelectedTask != null && !ReportPageVisibility);

        #endregion

        #region SpentCurrentTask

        public bool ShowTimeSpentDetailed { get; set; }

        public ICommand ShowCloseSpentCurrentTaskCommand { get => new DelegateCommand(() =>
        {
            ShowTimeSpentDetailed = !ShowTimeSpentDetailed;
        }, () => { return SelectedTask != null; });
        }

        #endregion

        #region Reports

        internal static event EventHandler<List<Models.JiraModel.Task>>? ShowReportEvent;
        internal static event EventHandler? HideReportEvent;

        public ICommand ShowCloseReportCommand
        {
            get => new DelegateCommand(() =>
        {
            ReportPageVisibility = !ReportPageVisibility;

            ShowCloseReportPage();
        });
        }

        private void ShowCloseReportPage()
        {
            if (ReportPageVisibility)
            {
                List<Models.JiraModel.Task> tasks = ListTasksView?.Cast<Models.JiraModel.Task>().ToList() ?? new List<Models.JiraModel.Task>();
                if (tasks.Any())
                {
                    ReportPage = new Views.ReportPage();

                    ShowReportEvent?.Invoke(null, tasks);
                    SelectedTask = null;
                }
                else
                {
                    ReportPageVisibility = false;
                    MessageBox.Show("Нет задач для вывода в отчет");
                }
            }
            else
                HideReportEvent?.Invoke(null, null);
        }

        public bool ReportPageVisibility { get; set; }

        public Page? ReportPage { get; set; } = new Views.ReportPage();

        #endregion

        private void InitFilterListTasks()
        {
            ListTasksView.Filter = FilterListTasks;
        }
        private bool FilterListTasks(object obj)
        {
            bool result = true;

            if (obj is Models.JiraModel.Task row)
            {
                // Check filter text
                if (result && !string.IsNullOrWhiteSpace(TextToFilterListTasks))
                {
                    result = TextToFilterContainsInFiels(row.Key)
                        || TextToFilterContainsInFiels(row.Status)
                        || TextToFilterContainsInFiels(row.Summary)
                        || TextToFilterContainsInFiels(row.ReporterUser?.Name)
                        || TextToFilterContainsInFiels(row.AssigneeUser?.Name);
                }

                // Check filter users
                if (result)
                {
                    result = FilterUsers.Any(el => el.IsChecked && (el.User == row.ReporterUser || el.User == row.AssigneeUser));
                }

                // Check statuses
                if (result)
                {
                    result = ListStatuses.Any(el => el.IsChecked && el.Name == row.Status);
                }
            }

            return result;
        }
        private bool TextToFilterContainsInFiels(string? value)
            => value?.Contains(TextToFilterListTasks, StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
