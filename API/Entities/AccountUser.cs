using System.Collections.Generic;
using System.Linq;
using API.Entities.DB;

namespace API.Entities {
	public class AccountUser {
        public string Username { get; set; }
        public string Token { get; set; }
        public ICollection<Photo> Photos { get; set; }
        public string KnownAs { get; set; }
        public string Gender { get; set; }

        public string GetPhotoUrl() {
            return Photos?.FirstOrDefault(photo => photo.IsMain)?.Url;
        }
	}
}