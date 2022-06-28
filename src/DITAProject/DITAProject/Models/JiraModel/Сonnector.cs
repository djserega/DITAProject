using Atlassian.Jira;
using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using LExp = System.Linq.Expressions;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.IO;
using System.Threading.Tasks;

namespace ITAJira.Models.JiraModel
{
    internal class Сonnector : BindableBase
    {
        private Jira? _jira;

        private const string _nameConfigJson = "config.json";

        public Сonnector()
        {
            IConfigurationRoot? _config = new ConfigurationBuilder()
                    .SetBasePath(new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName)
                    .AddJsonFile(_nameConfigJson, false, true)
                    .Build();

            Address = _config.GetValue<string>("address");
            User = _config.GetValue<string>("user");
            Project = _config.GetValue<string>("Project");
        }

        internal static event EventHandler<List<TaskStatus>>? ListStatusUpdatingEvent;
        internal static event EventHandler<List<Task>>? ListTaskUpdatingEvent;
        internal static event EventHandler<List<User>>? ListUsersInListTask;

        public string Address { get; set; }
        public string User { get; set; }
        private string Project { get; set; }

        public bool Connected { get; private set; }

        public bool ListTaskUpdating { get; private set; }

        public ICommand ConnectCommand => new DelegateCommand(() =>
        {
            Connected = false;

            Views.EnterPassword windowPwd = new()
            {
                Owner = Application.Current.MainWindow
            };

            if (windowPwd.ShowDialog() ?? false)
            {
                if (string.IsNullOrEmpty(windowPwd.GetPassword()))
                {
                    MessageBox.Show("Не указан api-ключ доступа.");
                }
                else
                {
                    string pwd = windowPwd.GetPassword();

                    TryConnecting(pwd);

                    if (Connected)
                    {
                        ViewModels.MainViewModel? mainViewModel = VMLoader.Resolve<ViewModels.MainViewModel>();
                        if (mainViewModel != null)
                        {
                            mainViewModel.ConnectorTime.Address = Address;
                            mainViewModel.ConnectorTime.User = User;
                            mainViewModel.ConnectorTime.TryConnecting(pwd);
                        }

                        GetStatuses();
                    }
                }
            }
        });

        public ICommand SetCurrentConnectionParameterToDefault => new DelegateCommand(() =>
        {
            FileInfo fileConfig = new(_nameConfigJson);

            StringBuilder builderWriter = new();
            using (StreamReader reader = new(fileConfig.OpenRead()))
            {
                do
                {
                    string? currentRowConfig = reader.ReadLine();

                    if (currentRowConfig != null)
                    {
                        if (currentRowConfig.Contains("address"))
                            currentRowConfig = $"    \"address\": \"{Address}\",";
                        else if (currentRowConfig.Contains("user"))
                            currentRowConfig = $"    \"user\": \"{User}\",";

                        builderWriter.AppendLine(currentRowConfig);
                    }

                } while (!reader.EndOfStream);

                reader.Dispose();
            }

            using (StreamWriter writer = new(fileConfig.FullName, false))
            {
                writer.Write(builderWriter.ToString());
                writer.Flush();
                writer.Dispose();
            }

        });

        private void TryConnecting(string pwd)
        {
            if (Address == "https://<domain>")
            {
                MessageBox.Show("Не указан адрес подключения");
                return;
            }

            try
            {
                _jira = Jira.CreateRestClient(Address, User, pwd);

                Connected = true;
            }
            catch (UriFormatException ex)
            {
                MessageBox.Show($"Ошибка подключения.\n{ex.Message}");
            }
        }

        private async void GetStatuses()
        {
            if (_jira == null)
                return;

            IEnumerable<IssueStatus> statuses;
            try
            {
                statuses = await _jira.Statuses.GetStatusesAsync();
            }
            catch (System.Security.Authentication.AuthenticationException ex)
            {
                if (ex.Message.Contains("Basic authentication with passwords is deprecated"))
                    MessageBox.Show("Не верный api-ключ");

                return;
            }

            TaskStatus.TaskStatuses.Clear();

            foreach (IssueStatus itemStatus in statuses)
                TaskStatus.TaskStatuses.Add(new(itemStatus));

            TaskStatus.TaskStatuses.Sort((a, b) => a.Name?.CompareTo(b.Name) ?? 0);

            ListStatusUpdatingEvent?.Invoke(null, TaskStatus.TaskStatuses);
        }

        public ICommand GetTaskCommand => new DelegateCommand(async () =>
        {
            await System.Threading.Tasks.Task.Run(() => GetTask());
        }, () => Connected && !ListTaskUpdating);

        private void GetTask()
        {
            ListTaskUpdating = true;

            IQueryable<Issue>? issues = GetFilteredIssue();

            if (issues == null)
            {
                ListTaskUpdating = false;
                return;
            }

            List<Task> listTask = new();
            foreach (Issue item in issues)
            {
                listTask.Add(new(item));
            }

            ListTaskUpdatingEvent?.Invoke(null, listTask);

            List<User?> usersTasks = new();
            usersTasks.AddRange(listTask.Select(el => el.AssigneeUser));
            usersTasks.AddRange(listTask.Select(el => el.ReporterUser));

            ListUsersInListTask?.Invoke(null, usersTasks.Distinct().Where(el => el != null).ToList());

            ListTaskUpdating = false;
        }

        internal IEnumerable<IssueChangeLog>? GetTimeSpentFromIssue(string? key)
        {
            if (key == null)
                return default;

            IQueryable<Issue> issues = GetFilteredIssue();

            if (issues == null)
                return default;

            IEnumerable<IssueChangeLog> listLogsIssue = null;
            foreach (Issue item in issues)
            {
                if (item.Key == key)
                {
                    listLogsIssue = item.GetChangeLogsAsync().GetAwaiter().GetResult();
                    break;
                }
            }

            return listLogsIssue;
        }

        private IQueryable<Issue>? GetFilteredIssue()
        {
            if (!Connected || _jira == null)
            {
                MessageBox.Show("Не подключено");
                return default;
            }

            ViewModels.MainViewModel? mainView = VMLoader.Resolve<ViewModels.MainViewModel>();

            if (mainView == null)
                return default;

            IQueryable<Issue>? listIssues = GetIssuesFromProject();
            if (listIssues == null)
                return default;
            listIssues = listIssues.Where(el => el.Updated >= mainView.DateTimeStartPeriod && el.Updated < mainView.DateTimeEndPeriod.AddDays(1));

            listIssues = SetFilterStatuses(mainView, listIssues);

            IOrderedQueryable<Issue> lastFilteredIssues = listIssues.OrderByDescending(el => el.Created);

            return lastFilteredIssues.Take(100);
        }

        private static IQueryable<Issue> SetFilterStatuses(ViewModels.MainViewModel mainView, IQueryable<Issue> listIssues)
        {
            IEnumerable<string?> selectedStatuses = mainView.ListStatuses.Where(el => el.IsChecked).Select(el => el.Id);

            if (selectedStatuses.Any() && selectedStatuses.Count() != mainView.ListStatuses.Count)
            {
                var parameterExpression = LExp.Expression.Parameter(typeof(Issue), "CheckStatus");

                var checkStatusExpression = LExp.Expression.Property(parameterExpression, "Status");

                LExp.Expression? exp = null;

                foreach (string? idStatus in selectedStatuses)
                {
                    if (exp == null)
                        exp = LExp.Expression.Equal(checkStatusExpression, LExp.Expression.Constant(idStatus));
                    else
                        exp = LExp.Expression.OrElse(exp, LExp.Expression.Equal(checkStatusExpression, LExp.Expression.Constant(idStatus)));
                }

                if (exp != null)
                {
                    var lambda = (LExp.Expression<Func<Issue, bool>>)LExp.Expression.Lambda(exp, parameterExpression);

                    listIssues = listIssues.Where(lambda);
                }
            }

            return listIssues;
        }

        private IQueryable<Issue>? GetIssuesFromProject()
        {
            if (_jira == null)
                return default;

            return _jira.Issues.Queryable.Where(el => el.Project == Project);
        }
    }
}
