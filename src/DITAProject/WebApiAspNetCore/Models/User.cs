using Atlassian.Jira;
using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApiAspNetCore.Models
{
    internal class User : BindableBase
    {
		internal string? AccountId { get; set; }
		internal string? AvatarUrl { get; set; }
		public string? Name { get; set; }
		internal string? Email { get; set; }
        public bool IsActive { get; set; }

		internal static List<User> Users { get; set; } = new();

		internal static User AddUser(JiraUser user)
        {
            //TODO: check used: AccountId or Key
            if (Users.Any(el => el.AccountId == user.Key))
				return Users.First(el => el.AccountId == user.Key);

			User newUser = new User()
				.FillUser(user);

			Users.Add(newUser);

			return newUser;
        }

        private User FillUser(JiraUser user)
        {
            //TODO: check used: AccountId or Key
            AccountId = user.Key;
			AvatarUrl = user.AvatarUrls.Large;
			Name = user.DisplayName;
			Email = user.Email;
			IsActive = user.IsActive;

			return this;
		}
	}
}
