namespace ClothingShop.Models.Enums
{
    public enum Gender
    {
        Male,
        Female,
        Other
    }

    public static class GenderExtensions
    {
        public static string ToVietnamese(this Gender gender)
        {
            return gender switch
            {
                Gender.Male => "Nam",
                Gender.Female => "Nữ",
                Gender.Other => "Khác",
                _ => "Khác"
            };
        }

        public static Gender FromVietnamese(string gender)
        {
            return gender switch
            {
                "Nam" => Gender.Male,
                "Nữ" => Gender.Female,
                "Khác" => Gender.Other,
                _ => Gender.Other
            };
        }
    }
}
