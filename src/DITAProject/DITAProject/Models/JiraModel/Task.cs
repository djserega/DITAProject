using Atlassian.Jira;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ITAJira.Models.JiraModel
{
    internal class Task
    {
        public Task()
        {
        }
        public Task(Issue issue) : this()
        {
            FillTask(issue);
        }

        public string? Key { get; set; }
        public string? ParentIssueKey { get; set; }
        public User? AssigneeUser { get; set; }
        public User? ReporterUser { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? ResolutionDate { get; set; }
        public DateTime? Updated { get; set; }
        // CustomFields
        public string? Summary { get; set; }
        public string? Description { get; set; }
        public string? JiraIdentifier { get; set; }
        // Priority	{Medium}	Atlassian.Jira.IssuePriority
        public string? ProjectKey { get; set; }
        public string? Status { get; set; }
        public string? TimeSpent { get; set; }
        // TimeTrackingData	{Atlassian.Jira.IssueTimeTrackingData}	Atlassian.Jira.IssueTimeTrackingData
        //Type	{История}	Atlassian.Jira.IssueType

        public ObservableCollection<TaskTimeSpentItem> TimeSpentDetailes { get; set; } = new ObservableCollection<TaskTimeSpentItem>();

        private void FillTask(Issue issue)
        {
            if (issue.AssigneeUser != null)
                AssigneeUser = User.AddUser(issue.AssigneeUser);
            if (issue.ReporterUser != null)
                ReporterUser = User.AddUser(issue.ReporterUser);
            Created = issue.Created;
            Description = issue.Description;
            Summary = issue.Summary;
            JiraIdentifier = issue.JiraIdentifier;
            Key = issue.Key.ToString();
            ParentIssueKey = issue.ParentIssueKey;
            ProjectKey = issue.Project;
            Status = issue.Status.Name;
            Updated = issue.Updated;
            TimeSpent = issue.TimeTrackingData.TimeSpent;
        }

        internal void FillTimeSpent(IEnumerable<IssueChangeLog> issueChangeLogs)
        {
            App.Current.Dispatcher.Invoke(delegate
            {
                TimeSpentDetailes.Clear();
            });

            foreach (IssueChangeLog itemChangeLog in issueChangeLogs.Where(el => el.Items.Any(el2 => el2.FieldName == "timespent")))
            {
                foreach (IssueChangeLogItem itemLog in itemChangeLog.Items)
                {
                    if (itemLog.FieldName == "timespent")
                    {
                        App.Current.Dispatcher.Invoke(delegate
                        {
                            TimeSpentDetailes.Add(new(itemChangeLog.Author, itemChangeLog.CreatedDate, itemLog));
                        });
                    }
                }
            }
        }
    }
}
