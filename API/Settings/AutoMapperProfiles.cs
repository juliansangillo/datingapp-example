using System.Linq;
using API.Entities;
using API.Entities.DB;
using API.Entities.DTOs;
using API.Extensions;
using AutoMapper;

namespace API.Settings {
	public class AutoMapperProfiles : Profile {
		public AutoMapperProfiles() {
            CreateMap<RegisterDto, AppUser>();
            CreateMap<AppUser, UserDto>()
                .ForMember(u => u.PhotoUrl, conf => conf.MapFrom(au => au.Photos.FirstOrDefault(photo => photo.IsMain).Url));

            CreateMap<AppUser, UserWithRoles>()
                .ForMember(ur => ur.Roles, conf => conf.MapFrom(au => au.UserRoles.Select(r => r.Role.Name).ToList()));

            CreateMap<AppUser, BasicUser>();
            CreateMap<AppUser, LikeDto>()
                .ForMember(l => l.PhotoUrl, conf => conf.MapFrom(au => au.Photos.FirstOrDefault(photo => photo.IsMain).Url))
                .ForMember(l => l.Age, conf => conf.MapFrom(au => au.DateOfBirth.CalculateAge()));
            
            CreateMap<AppUser, MemberDto>()
                .ForMember(m => m.PhotoUrl, conf => conf.MapFrom(au => au.Photos.FirstOrDefault(photo => photo.IsMain).Url))
                .ForMember(m => m.Age, conf => conf.MapFrom(au => au.DateOfBirth.CalculateAge()));
			CreateMap<MemberUpdateDto, AppUser>();
            
            CreateMap<Photo, PhotoDto>();
            CreateMap<Photo, PhotoForApprovalDto>()
                .ForMember(dto => dto.Username, conf => conf.MapFrom(p => p.AppUser.UserName));

            CreateMap<Message, MessageDto>()
                .ForMember(md => md.SenderPhotoUrl, conf => conf.MapFrom(m => m.Sender.Photos.FirstOrDefault(photo => photo.IsMain).Url))
                .ForMember(md => md.RecipientPhotoUrl, conf => conf.MapFrom(m => m.Recipient.Photos.FirstOrDefault(photo => photo.IsMain).Url));
		}
	}
}