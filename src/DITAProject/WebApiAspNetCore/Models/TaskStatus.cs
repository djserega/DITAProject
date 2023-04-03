using Atlassian.Jira;
using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WebApiAspNetCore.Models
{
    internal class TaskStatus : BindableBase
    {
        internal static event EventHandler<string>? ChangeCheckMarkStatusEvent;
  
        public TaskStatus()
        {
        }
        public TaskStatus(IssueStatus status)
        {
            IsChecked = true;
            IconUrl = status.IconUrl;
            Id = status.Id;
            Name = status.Name;
        }

        public bool IsChecked { get; set; }
        public string? IconUrl { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }

        public static List<TaskStatus> TaskStatuses = new();

        public ICommand ChandeCheckMarkCommand => new DelegateCommand<string>((string name) => { ChangeCheckMarkStatusEvent?.Invoke(null, name); });

    }
}
