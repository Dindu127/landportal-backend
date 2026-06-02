using System;
using System.ComponentModel.DataAnnotations.Schema;
using LandPortal.Api.Enums;

namespace LandPortal.Api.Entities
{
    public class Property
    {

        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("owner_id")]
        public Guid OwnerId { get; set; }

        [Column("title")]
        public string Title { get; set; } = default!;

        [Column("description")]
        public string Description { get; set; } = default!;

        [Column("price")]
        public decimal Price { get; set; }   // decimal(18,2)

        [Column("city")]
        public string City { get; set; } = default!;

        [Column("locality")]
        public string Locality { get; set; } = default!;

        [Column("land_size")]
        public decimal LandSize { get; set; } // decimal(18,2)

        [Column("size_unit")]
        public SizeUnit SizeUnit { get; set; } = SizeUnit.Sqft;

        //[Column("status")]
        //public PropertyStatus Status { get; set; } = PropertyStatus.Draft;

        //[Column("size_unit")]
        //public string SizeUnit { get; set; } = "Sqft";

        [Column("status")]
        public string Status { get; set; } = "Draft";


        [Column("listed_at")]
        public DateTime? ListedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("is_featured")]
        public bool IsFeatured { get; set; } = false;

        [Column("cover_image_url")]
        public string? CoverImageUrl { get; set; }


        // nav

        [Column("owner")]
        public User Owner { get; set; } = default!;

        [Column("media")]
        public ICollection<PropertyMedia> Media { get; set; } = new List<PropertyMedia>();

        [Column("road_access")]
        public string? RoadAccess { get; set; }   // e.g. "20 ft tar road"

        [Column("facing")]
        public string? Facing { get; set; }       // e.g. "East", "North-East"

        [Column("plot_type")]
        public string? PlotType { get; set; }     // e.g. "DTCP Approved"

        [Column("brokerage")]
        public string? Brokerage { get; set; }    // e.g. "No brokerage" / "2%"

        [Column("is_sold")]
        public bool IsSold { get; set; }

        [Column("sold_at")]
        public DateTime? SoldAt { get; set; }

        [Column("sold_by_id")]
        public Guid? SoldById { get; set; }


    }
}
