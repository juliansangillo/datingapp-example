using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace API.Entities.DB {
	public class AppRole : IdentityRole<int> {
        public ICollection<AppUserRole> UserRoles { get; set; }
	}
}