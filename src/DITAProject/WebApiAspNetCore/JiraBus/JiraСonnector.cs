using Atlassian.Jira;
using DevExpress.Mvvm;
using LExp = System.Linq.Expressions;
using System.Text;
using System.Windows.Input;

namespace WebApiAspNetCore.JiraBus
{
    internal class JiraConnector : BindableBase
    {
        private readonly Config _config;
        private readonly Logger _logger;

        private Jira? _jira;

        public JiraConnector(Config config, Logger logger)
        {
            _config = config;
            _logger = logger;

            Address = _config.Address;
            User = _config.User;
        }

        internal static event EventHandler<List<TaskStatus>>? ListStatusUpdatingEvent;
        internal static event EventHandler<List<Task>>? ListTaskUpdatingEvent;
        internal static event EventHandler<List<JiraUser>>? ListUsersInListTask;

        internal static event EventHandler<bool>? InvokeConnectedCommandEvent;

        public string? Address { get; set; }
        public string? User { get; set; }

        public bool IsConnected { get; private set; }

        public bool ListTaskUpdating { get; private set; }

        public async Task ConnectAsync(string password)
        {
            IsConnected = false;

            await TryConnecting(password);

            GetStatuses();

            InvokeConnectedCommandEvent?.Invoke(null, IsConnected);
        }

        public ICommand SetCurrentConnectionParameterToDefault => new DelegateCommand(() =>
        {
            FileInfo fileConfig = new(_config.NameConfig);

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

            if (Address == "https://<domain>")
            {
                _logger.Inf("Not filled address resource");
                throw new Exception("Не указан адрес подключения");
            }

            bool result;

            try
            {
                _logger.Inf("Connecting");

                _jira = Jira.CreateRestClient(Address, User, pwd);

                await _jira.Statuses.GetStatusesAsync();

                IsConnected = true;
                result = true;

                _logger.Inf("Success");
            }
            catch (UriFormatException ex)
            {
                _logger.Err($"Connecting error:\n{ex}");
                throw new Exception($"Ошибка подключения.\n{ex.Message}");
            }
            catch (System.Security.Authentication.AuthenticationException ex)
            {
                _logger.Err($"Connecting error:\n{ex}");
                if (ex.Message.Contains("Basic authentication with passwords is deprecated"))
                    throw new Exception("Не верный api-ключ");
                else
                    throw new Exception("Неизвестная ошибка авторизации");
            }
            catch (Exception ex)
            {
                _logger.Err($"Connecting error:\n{ex}");
                throw new Exception("Ошибка подключения");
            }

            return result;
        }

        private async void GetStatuses()
        {
            if (_jira == null)
                return;

            _logger.Inf("Loading statuses");

            IEnumerable<IssueStatus> statuses = await _jira.Statuses.GetStatusesAsync();

            _logger.Inf($"Loaded statuses: {statuses.Count()}");

            Models.TaskStatus.TaskStatuses.Clear();

            foreach (IssueStatus itemStatus in statuses)
                Models.TaskStatus.TaskStatuses.Add(new(itemStatus));

            Models.TaskStatus.TaskStatuses.Sort((a, b) => a.Name?.CompareTo(b.Name) ?? 0);

            //ListStatusUpdatingEvent?.Invoke(null, Models.TaskStatus.TaskStatuses);
        }

        internal List<Models.Task>? GetTask()
        {
            ListTaskUpdating = true;

            _logger.Inf("Loading issues");

            IQueryable<Issue>? issues = GetFilteredIssue();

            if (issues == null)
            {
                ListTaskUpdating = false;
                return default;
            }

            _logger.Inf("Converting issues");

            List<Models.Task> listTask = new();
            foreach (Issue item in issues)
            {
                listTask.Add(new(item));
            }

            //ListTaskUpdatingEvent?.Invoke(null, listTask);

            _logger.Inf("Updating list users");

            List<Models.User?> usersTasks = new();
            usersTasks.AddRange(listTask.Select(el => el.AssigneeUser));
            usersTasks.AddRange(listTask.Select(el => el.ReporterUser));

            //ListUsersInListTask?.Invoke(null, usersTasks.Distinct().Where(el => el != null).ToList());

            ListTaskUpdating = false;

            return listTask;
        }

        internal IEnumerable<IssueChangeLog>? GetTimeSpentFromIssue(string? key)
        {
            if (key == null)
                return default;

            _logger.Inf($"Loading timespent issue {key}");

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
            if (!IsConnected || _jira == null)
            {
                throw new Exception("Не подключено");
            }

            //ViewModels.MainViewModel? mainView = VMLoader.Resolve<ViewModels.MainViewModel>();

            //if (mainView == null)
            //    return default;

            _logger.Inf("Initializing base filter issues");

            IQueryable<Issue>? listIssues = GetIssuesFromProject();
            if (listIssues == null)
                return default;

            _logger.Inf("Filtering by date");

            //listIssues = listIssues.Where(el => el.Updated >= mainView.DateTimeStartPeriod && el.Updated < mainView.DateTimeEndPeriod.AddDays(1));

            _logger.Inf("Filtering by statuses");

            //listIssues = SetFilterStatuses(mainView, listIssues);

            _logger.Inf("Sorted elements");

            IOrderedQueryable<Issue> lastFilteredIssues = listIssues.OrderByDescending(el => el.Created);

            _logger.Inf("Loading issues");

            IQueryable<Issue> issues = lastFilteredIssues.Take(200);

            try
            {
                _logger.Inf($"Loaded issues: {issues.Count()}");
            }
            catch (AggregateException ex)
            {
                _logger.Err(ex.ToString());
                throw new Exception($"Ошибка загрузки задач:\n{ex.InnerException?.Message}");
            }

            return issues;
        }

        private static IQueryable<Issue> SetFilterStatuses(IEnumerable<string?> selectedStatuses, IQueryable<Issue> listIssues)
        {
            if (selectedStatuses.Any())
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

            if (_config.Projects.Any())
                return _jira.Issues.Queryable.Where(
                    el =>

                    el.Project == _config.Projects[0]
                    || el.Project == _config.Projects[1]

                    );
            else
                return _jira.Issues.Queryable;
        }
    }
}
