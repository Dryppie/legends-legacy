namespace Application.UseCases.Users;

public static class GuestCharacterNameGenerator
{
    public static string Generate()
    {
        var prefixes = new[]
            {
                "Silent", "Swift", "Mighty", "Lucky", "Clever", "Brave", "Gentle", "Fierce", "Bold", "Wild",
                "Calm", "Stormy", "Vivid", "Bright", "Dark", "Noble", "Proud", "Shy", "Quick", "Sly",
                "Lone", "Nimble", "Radiant", "Wise", "Cheerful", "Eager", "Mystic", "Fearless", "Daring", "Joyful",
                "Steady", "Thunder", "Majestic", "Silent", "Sparkling", "Serene", "Stout", "Loyal", "Iron", "Fiery"
            };

        var animals = new[]
        {
                "Fox", "Bear", "Tiger", "Hawk", "Lion", "Wolf", "Otter", "Eagle", "Panther", "Falcon",
                "Raven", "Shark", "Puma", "Cobra", "Jaguar", "Leopard", "Bison", "Lynx", "Cougar", "Phoenix",
                "Owl", "Dragon", "Unicorn", "Griffin", "Badger", "Cheetah", "Stag", "Rhino", "Lizard",
                "Antelope", "Gazelle", "Ram", "Horse", "Buffalo", "Beetle", "Whale", "Spider", "Wolverine", "Elephant"
            };

        var suffixes = new[]
        {
                "Walker", "Seeker", "Rider", "Hunter", "Keeper", "Wanderer", "Dreamer", "Protector", "Guardian", "Voyager",
                "Strider", "Glider", "Howler", "Whisperer", "Tracker", "Scout", "Mage", "Sentinel", "Scribe", "Knight",
                "Mystic", "Sailor", "Pathfinder", "Warrior", "Champion", "Explorer", "Savior", "Adventurer", "Defender", "Scout",
                "Challenger", "Nomad", "Beholder", "Sorcerer", "Alchemist", "Master", "Scribe", "Scholar", "Pilgrim", "Ranger"
            };

        var prefix = prefixes[Random.Shared.Next(prefixes.Length)];
        var animal = animals[Random.Shared.Next(animals.Length)];
        var suffix = suffixes[Random.Shared.Next(suffixes.Length)];

        return $"{prefix}{animal}{suffix}_{Random.Shared.Next(1000, 9999)}";
    }
}
