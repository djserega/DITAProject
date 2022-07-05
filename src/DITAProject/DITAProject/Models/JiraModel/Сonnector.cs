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

        public Сonnector()
        {
            Address = Config.Address;
            User = Config.User;
        }

        internal static event EventHandler<List<TaskStatus>>? ListStatusUpdatingEvent;
        internal static event EventHandler<List<Task>>? ListTaskUpdatingEvent;
        internal static event EventHandler<List<User>>? ListUsersInListTask;

        public string Address { get; set; }
        public string User { get; set; }

        public bool Connected { get; private set; }

        public bool ListTaskUpdating { get; private set; }

        public ICommand ConnectCommand => new DelegateCommand(async () =>
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

                    await TryConnecting(pwd);

                    if (Connected)
                    {
                        ViewModels.MainViewModel? mainViewModel = VMLoader.Resolve<ViewModels.MainViewModel>();
                        if (mainViewModel != null)
                        {
                            mainViewModel.ConnectorTime.Address = Address;
                            mainViewModel.ConnectorTime.User = User;
                            await mainViewModel.ConnectorTime.TryConnecting(pwd);
                        }

                        GetStatuses();
                    }
                }
            }
        });

        public ICommand SetCurrentConnectionParameterToDefault => new DelegateCommand(() =>
        {
            FileInfo fileConfig = new(Config.NameConfig);

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

            using StreamWriter writer = new(fileConfig.FullName, false);
            writer.Write(builderWriter.ToString());
            writer.Flush();
            writer.Dispose();

        });

        private async Task<bool> TryConnecting(string pwd)
        {
            bool result = false;

            if (Address == "https://<domain>")
            {
                MessageBox.Show("Не указан адрес подключения");
                return result;
            }

            try
            {
                Logger.Inf("Connecting");

                _jira = Jira.CreateRestClient(Address, User, pwd);

                await _jira.Statuses.GetStatusesAsync();

                Connected = true;
                result = true;

                Logger.Inf("Success");
            }
            catch (UriFormatException ex)
            {
                Logger.Err($"Connecting error:\n{ex}");
                MessageBox.Show($"Ошибка подключения.\n{ex.Message}");
            }
            catch (System.Security.Authentication.AuthenticationException ex)
            {
                Logger.Err($"Connecting error:\n{ex}");
                if (ex.Message.Contains("Basic authentication with passwords is deprecated"))
                    MessageBox.Show("Не верный api-ключ");
            }
            catch(Exception ex)
            {
                Logger.Err($"Connecting error:\n{ex}");
                MessageBox.Show("Ошибка подключения");
            }

            return result;
        }

        private async void GetStatuses()
        {
            if (_jira == null)
                return;

            Logger.Inf("Loading statuses");

            IEnumerable<IssueStatus> statuses = await _jira.Statuses.GetStatusesAsync();

            Logger.Inf($"Loaded statuses: {statuses.Count()}");

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

            Logger.Inf("Loading issues");

            IQueryable<Issue>? issues = GetFilteredIssue();

            if (issues == null)
            {
                ListTaskUpdating = false;
                return;
            }

            Logger.Inf("Converting issues");

            List<Task> listTask = new();
            foreach (Issue item in issues)
            {
                listTask.Add(new(item));
            }

            ListTaskUpdatingEvent?.Invoke(null, listTask);

            Logger.Inf("Updating list users");

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

            Logger.Inf($"Loading timespent issue {key}");

            IQueryable<Issue>? issues = GetFilteredIssue();

            if (issues == null)
                return default;

            IEnumerable<IssueChangeLog>? listLogsIssue = null;
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

            Logger.Inf("Initializing base filter issues");

            IQueryable<Issue>? listIssues = GetIssuesFromProject();
            if (listIssues == null)
                return default;

            Logger.Inf("Filtering by date");

            listIssues = listIssues.Where(el => el.Updated >= mainView.DateTimeStartPeriod && el.Updated < mainView.DateTimeEndPeriod.AddDays(1));

            Logger.Inf("Filtering by statuses");

            listIssues = SetFilterStatuses(mainView, listIssues);

            Logger.Inf("Sorted elements");

            IOrderedQueryable<Issue> lastFilteredIssues = listIssues.OrderByDescending(el => el.Created);

            Logger.Inf("Loading issues");

            IQueryable<Issue> issues = lastFilteredIssues.Take(200);

            Logger.Inf($"Loaded issues: {issues.Count()}");

            return issues;
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

            return _jira.Issues.Queryable.Where(el => el.Project == Config.Project);
        }
    }
}
