using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ITAJira.Models
{
    internal class FilterUser : BindableBase
    {
        internal static event EventHandler<string>? ChangeCheckMarkUserEvent;

        public FilterUser(JiraModel.User user)
        {
            User = user;
        }

        public bool IsChecked { get; set; } = true;
        public JiraModel.User User { get; set; }

        public ICommand ChandeCheckMarkCommand => new DelegateCommand<string>((string name) => { ChangeCheckMarkUserEvent?.Invoke(null, name); });
    }
}
