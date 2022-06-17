using Atlassian.Jira;
using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

namespace ITAJira.Models.JiraModel
{
    internal class User : BindableBase
    {
		internal string? AccountId { get; set; }
		internal string? AvatarUrl { get; set; }
		public BitmapImage AvatarImage { get => new(new Uri(AvatarUrl ?? string.Empty)); } 
		public string? Name { get; set; }
		internal string? Email { get; set; }
        public bool IsActive { get; set; }

		internal static List<User> Users { get; set; } = new();

		internal static User AddUser(JiraUser user)
        {
			if (Users.Any(el => el.AccountId == user.AccountId))
				return Users.First(el => el.AccountId == user.AccountId);

			User newUser = new User()
				.FillUser(user);

			Users.Add(newUser);

			return newUser;
        }

        private User FillUser(JiraUser user)
        {
			AccountId = user.AccountId;
			AvatarUrl = user.AvatarUrls.Large;
			Name = user.DisplayName;
			Email = user.Email;
			IsActive = user.IsActive;

			return this;
		}
	}
}
