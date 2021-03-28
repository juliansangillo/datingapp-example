using System;
using System.Collections.Generic;
using System.Linq;
using API.Extensions;
using Microsoft.AspNetCore.Identity;

namespace API.Entities.DB {
    public class AppUser : IdentityUser<int> {
        public DateTime DateOfBirth { get; set; }
        public string KnownAs { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime LastActive { get; set; } = DateTime.Now;
        public string Gender { get; set; }
        public string Introduction { get; set; }
        public string LookingFor { get; set; }
        public string Interests { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public ICollection<Photo> Photos { get; set; }

        public ICollection<UserLike> LikedBy { get; set; }
        public ICollection<UserLike> Likes { get; set; }

        public ICollection<Message> MessagesSent { get; set; }
        public ICollection<Message> MessagesReceived { get; set; }

        public ICollection<AppUserRole> UserRoles { get; set; }

        public string GetPhotoUrl() {
            return Photos?.FirstOrDefault(photo => photo.IsMain)?.Url;
        }

        public int GetAge() {
            return DateOfBirth.CalculateAge();
        }
    }
}