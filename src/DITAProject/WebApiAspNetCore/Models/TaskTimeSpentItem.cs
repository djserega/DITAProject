using Atlassian.Jira;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApiAspNetCore.Models
{
    internal class TaskTimeSpentItem
    {
        internal TaskTimeSpentItem(JiraUser user, DateTime date, IssueChangeLogItem timeSpent)
        {
            if (user != null)
                User = User.AddUser(user);
            Date = date;

            int.TryParse(timeSpent.FromValue, out int oldValue);
            int.TryParse(timeSpent.ToValue, out int newValue);

            SpentSecond = newValue - oldValue;
        }

        public User? User { get; set; }
        public DateTime Date { get; set; }
        public int SpentSecond { get; set; }
        public string Spent
        {
            get
            {
                int min = 0;
                int hours = SpentSecond / 3600;
                if (hours * 3600 != SpentSecond)
                {
                    min = (SpentSecond - hours * 3600) / 60;
                    if (min < 1)
                        min = 0;
                }

                return $"{hours} ч.{(min == 0 ? "" : $" {min} мин.")}";
            }
        }
    }
}
