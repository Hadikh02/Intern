using AutoMapper;
using Intern.DTOs;
using Intern.Models;

namespace Intern.Mapping
{
    public class MappingProfile :Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>().ReverseMap();
            CreateMap<Room, RoomDto>().ReverseMap();
            CreateMap<Meeting, MeetingDto>().ReverseMap();
            CreateMap<MeetingAttendee, MeetingAttendeeDto>().ReverseMap();
            CreateMap<Minute, MinuteDto>().ReverseMap();
            CreateMap<Agenda, AgendaDto>().ReverseMap();
            CreateMap<Notification, NotificationDto>().ReverseMap();
        }
    }
}
