using AutoMapper;
using StokApi.Models;

namespace StokApi.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductDto>();

        CreateMap<CreateProductDto, Product>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name.Trim()));

        CreateMap<UpdateProductDto, Product>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name.Trim()));
    }
}