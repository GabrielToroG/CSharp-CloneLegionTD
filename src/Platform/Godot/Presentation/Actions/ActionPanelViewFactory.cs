using System.Collections.Generic;
using Godot;
using LegionTDClone.Domain.Match;
using LegionTDClone.Domain.Roster;
using LegionTDClone.Platform.Godot.Simulation;
using LegionTDClone.Queries.Economy;

namespace LegionTDClone.Platform.Godot.Presentation.Actions
{
    public static class ActionPanelViewFactory
    {
        public static IReadOnlyList<ActionSlotViewData> Build(
            Node3D selectedNode,
            bool isConstructorBuildMenuOpen,
            int selectedConstructorTowerIndex,
            EconomyQueryService economyQuery,
            RosterState rosterState)
        {
            var slots = new List<ActionSlotViewData>
            {
                Empty(), Empty(), Empty(), Empty(), Empty(), Empty()
            };

            if (selectedNode is TowerAdapter tower && tower.Data != null)
            {
                TeamSide team = ResolveTeamFromNode(tower);
                int refund = Mathf.RoundToInt(tower.Data.Cost * tower.Data.SellRefundFactor);
                bool hasUpgrade = tower.Data.UpgradeScene != null && tower.Data.UpgradeCost > 0;

                slots[0] = new ActionSlotViewData
                {
                    Visible = true,
                    Disabled = false,
                    Text = $"Vender\n+{refund}g"
                };

                slots[1] = new ActionSlotViewData
                {
                    Visible = true,
                    Disabled = !hasUpgrade || economyQuery.GetGold(team) < tower.Data.UpgradeCost,
                    Text = hasUpgrade ? $"Mejorar\n{tower.Data.UpgradeCost}g" : "Mejorar\nN/A"
                };

                return slots;
            }

            if (selectedNode is ConstructorAdapter)
            {
                if (isConstructorBuildMenuOpen)
                {
                    TeamSide team = ResolveTeamFromNode(selectedNode);
                    int teamGold = economyQuery.GetGold(team);
                    int blueCost = rosterState.GetTowerCost(0);
                    int greenCost = rosterState.GetTowerCost(1);

                    slots[0] = new ActionSlotViewData
                    {
                        Visible = true,
                        Disabled = teamGold < blueCost,
                        Text = $"Torre Azul\n{blueCost}g",
                        HighlightColor = selectedConstructorTowerIndex == 0 ? new Color(0.25f, 0.75f, 1f, 1f) : null
                    };

                    slots[1] = new ActionSlotViewData
                    {
                        Visible = true,
                        Disabled = teamGold < greenCost,
                        Text = $"Torre Verde\n{greenCost}g",
                        HighlightColor = selectedConstructorTowerIndex == 1 ? new Color(0.25f, 1f, 0.4f, 1f) : null
                    };

                    slots[2] = new ActionSlotViewData
                    {
                        Visible = true,
                        Disabled = false,
                        Text = "Volver"
                    };

                    return slots;
                }

                slots[0] = new ActionSlotViewData
                {
                    Visible = true,
                    Disabled = false,
                    Text = "Construir"
                };
            }

            return slots;
        }

        private static ActionSlotViewData Empty() => new();

        private static TeamSide ResolveTeamFromNode(Node3D node)
        {
            Node current = node;
            while (current != null)
            {
                string name = current.Name.ToString();
                if (name == "Lane_Right") return TeamSide.Right;
                if (name == "Lane_Left") return TeamSide.Left;
                current = current.GetParent();
            }

            return TeamSide.Left;
        }
    }
}
