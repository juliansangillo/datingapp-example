using System.Collections.Generic;

namespace API.Entities.DTOs {
	public class UserWithRoles {
        public int Id { get; set; }
        public string Username { get; set; }
        public IList<string> Roles { get; set; }
	}
}