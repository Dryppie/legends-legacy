using Application.Interfaces.Services.LL.Rewards;
using Domain.Models.Rewards;
using Services.LL.Interfaces.Combat.Reward;
using Services.LL.Rewards;

namespace EssenceSystem.Tests;

public sealed class RewardRollerTests
{
    [Fact]
    public void Weighted_with_no_drop_can_return_zero_items()
    {
        var roller = CreateRoller(
            new SequenceRandomSource(0.50),
            new RewardTableDefinition
            {
                Id = "table",
                Rolls =
                [
                    new()
                    {
                        Id = "roll",
                        Type = RewardRollType.WeightedWithNoDrop,
                        NoDropWeight = 90,
                        Entries =
                        [
                            new()
                            {
                                Id = "rare_item",
                                Type = RewardEntryType.Item,
                                ItemId = "rare_item",
                                Weight = 10
                            }
                        ]
                    }
                ]
            });

        var result = roller.Roll("table", new RewardRollContext("test"));

        Assert.Empty(result.Items);
        Assert.Contains(result.Trace, trace => trace.Outcome == "no-drop");
    }

    [Fact]
    public void Weighted_roll_returns_exactly_one_selected_item()
    {
        var roller = CreateRoller(
            new SequenceRandomSource(0.75),
            new RewardTableDefinition
            {
                Id = "table",
                Rolls =
                [
                    new()
                    {
                        Id = "roll",
                        Type = RewardRollType.Weighted,
                        Entries =
                        [
                            new()
                            {
                                Id = "ore",
                                Type = RewardEntryType.Item,
                                ItemId = "ore",
                                Weight = 50
                            },
                            new()
                            {
                                Id = "gem",
                                Type = RewardEntryType.Item,
                                ItemId = "gem",
                                Weight = 50
                            }
                        ]
                    }
                ]
            });

        var item = Assert.Single(roller.Roll("table", new RewardRollContext("test")).Items);

        Assert.Equal("gem", item.ItemId);
        Assert.Equal(1, item.Quantity);
    }

    [Fact]
    public void Independent_roll_can_return_multiple_items()
    {
        var roller = CreateRoller(
            new SequenceRandomSource(0.10, 0.20, 0.95),
            new RewardTableDefinition
            {
                Id = "table",
                Rolls =
                [
                    new()
                    {
                        Id = "roll",
                        Type = RewardRollType.Independent,
                        Entries =
                        [
                            new()
                            {
                                Id = "ore",
                                Type = RewardEntryType.Item,
                                ItemId = "ore",
                                Chance = 0.50
                            },
                            new()
                            {
                                Id = "stone",
                                Type = RewardEntryType.Item,
                                ItemId = "stone",
                                Chance = 0.50
                            },
                            new()
                            {
                                Id = "gem",
                                Type = RewardEntryType.Item,
                                ItemId = "gem",
                                Chance = 0.50
                            }
                        ]
                    }
                ]
            });

        var result = roller.Roll("table", new RewardRollContext("test"));

        Assert.Equal(["ore", "stone"], result.Items.Select(x => x.ItemId));
    }

    [Fact]
    public void Sequence_roll_can_combine_referenced_tables()
    {
        var roller = CreateRoller(
            new SequenceRandomSource(),
            new RewardTableDefinition
            {
                Id = "parent",
                Rolls =
                [
                    new()
                    {
                        Id = "sequence",
                        Type = RewardRollType.Sequence,
                        Entries =
                        [
                            new()
                            {
                                Id = "basic_ref",
                                Type = RewardEntryType.RewardTableReference,
                                RewardTableId = "basic"
                            },
                            new()
                            {
                                Id = "bonus_ref",
                                Type = RewardEntryType.RewardTableReference,
                                RewardTableId = "bonus"
                            }
                        ]
                    }
                ]
            },
            new RewardTableDefinition
            {
                Id = "basic",
                Rolls =
                [
                    new()
                    {
                        Id = "all",
                        Type = RewardRollType.All,
                        Entries =
                        [
                            new() { Id = "ore", Type = RewardEntryType.Item, ItemId = "ore" }
                        ]
                    }
                ]
            },
            new RewardTableDefinition
            {
                Id = "bonus",
                Rolls =
                [
                    new()
                    {
                        Id = "all",
                        Type = RewardRollType.All,
                        Entries =
                        [
                            new() { Id = "gem", Type = RewardEntryType.Item, ItemId = "gem" }
                        ]
                    }
                ]
            });

        var result = roller.Roll("parent", new RewardRollContext("test"));

        Assert.Equal(["ore", "gem"], result.Items.Select(x => x.ItemId));
    }

    [Fact]
    public void Excluded_roll_ids_are_skipped_without_affecting_other_rolls()
    {
        var roller = CreateRoller(
            new SequenceRandomSource(),
            new RewardTableDefinition
            {
                Id = "table",
                Rolls =
                [
                    new()
                    {
                        Id = "blueprint_drop",
                        Type = RewardRollType.All,
                        Entries =
                        [
                            new() { Id = "blueprint", Type = RewardEntryType.Item, ItemId = "blueprint" }
                        ]
                    },
                    new()
                    {
                        Id = "tool_drop",
                        Type = RewardRollType.All,
                        Entries =
                        [
                            new() { Id = "tool", Type = RewardEntryType.Item, ItemId = "tool" }
                        ]
                    }
                ]
            });

        var result = roller.Roll(
            "table",
            new RewardRollContext(
                "test",
                ExcludedRollIds: new HashSet<string>(["blueprint_drop"], StringComparer.OrdinalIgnoreCase)));

        Assert.Equal(["tool"], result.Items.Select(item => item.ItemId));
        Assert.Contains(result.Trace, trace =>
            trace.RollId == "blueprint_drop" && trace.Outcome == "roll-excluded");
    }

    private static RewardRoller CreateRoller(IRandomSource random, params RewardTableDefinition[] definitions) =>
        new(new StaticRewardTableProvider(definitions), random);

    private sealed class StaticRewardTableProvider : IRewardTableDefinitionProvider
    {
        private readonly IReadOnlyDictionary<string, RewardTableDefinition> _definitions;

        public StaticRewardTableProvider(IReadOnlyCollection<RewardTableDefinition> definitions)
        {
            _definitions = definitions.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        }

        public RewardTableDefinition GetById(string id) => _definitions[id];
        public RewardTableDefinition? FindById(string id) => _definitions.GetValueOrDefault(id);
        public IReadOnlyList<RewardTableDefinition> GetAll() => _definitions.Values.ToList();
    }

    private sealed class SequenceRandomSource : IRandomSource
    {
        private readonly Queue<double> _values;

        public SequenceRandomSource(params double[] values)
        {
            _values = new Queue<double>(values);
        }

        public double NextDouble() => _values.Count == 0 ? 0.0 : _values.Dequeue();
    }
}
