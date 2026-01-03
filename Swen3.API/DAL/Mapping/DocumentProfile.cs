using AutoMapper;
using Swen3.API.DAL.DTOs;
using Swen3.API.DAL.Models;

namespace Swen3.API.DAL.Mapping
{
    public class DocumentProfile : Profile
    {
        public DocumentProfile()
        {
            // Document -> DocumentDto
            CreateMap<Document, DocumentDto>()
                .ForMember(dest => dest.PriorityName, opt => opt.MapFrom(src => src.Priority != null ? src.Priority.Name : null));
            
            // DocumentDto -> Document (for creating new documents)
            CreateMap<DocumentDto, Document>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Let EF generate the ID
                .ForMember(dest => dest.UploadedAt, opt => opt.Ignore()) // Let EF set the timestamp
                .ForMember(dest => dest.StorageKey, opt => opt.Ignore())
                .ForMember(dest => dest.Priority, opt => opt.Ignore());

            // Priority -> PriorityDto
            CreateMap<Priority, PriorityDto>();
        }
    }
}
