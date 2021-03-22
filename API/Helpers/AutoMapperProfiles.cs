using System;
using API.DTOs;
using API.Entities;
using AutoMapper;

namespace API.Helpers {
	public class AutoMapperProfiles : Profile {
		public AutoMapperProfiles() {
            CreateMap<AppUser, MemberDto>();
            CreateMap<AppUser, LikeDto>();
			CreateMap<MemberUpdateDto, AppUser>();
            CreateMap<Photo, PhotoDto>();
			CreateMap<RegisterDto, AppUser>();
            CreateMap<Message, MessageDto>();
            CreateMap<DateTime, DateTime>().ConvertUsing(d => DateTime.SpecifyKind(d, DateTimeKind.Utc));
		}
	}
}