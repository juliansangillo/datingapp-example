using API.Entities;
using API.Entities.DB;
using API.Entities.DTOs;
using AutoMapper;

namespace API.Settings {
	public class AutoMapperProfiles : Profile {
		public AutoMapperProfiles() {
            CreateMap<RegisterDto, AppUser>();
            CreateMap<AppUser, UserDto>();

            CreateMap<AppUser, BasicUser>();
            CreateMap<AppUser, LikeDto>();
            
            CreateMap<AppUser, MemberDto>();
			CreateMap<MemberUpdateDto, AppUser>();
            CreateMap<Photo, PhotoDto>();
            CreateMap<Message, MessageDto>();
		}
	}
}