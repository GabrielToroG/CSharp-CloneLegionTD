using Godot;
using LegionTDClone.Domain.Match;
using LegionTDClone.Domain.Roster;
using LegionTDClone.Platform.Godot.Presentation.Actions;
using LegionTDClone.Platform.Godot.Presentation.Selection;
using LegionTDClone.Platform.Godot.Input;
using LegionTDClone.Platform.Godot.Simulation;
using LegionTDClone.Queries.Economy;
using LegionTDClone.Queries.Match;

namespace LegionTDClone.Platform.Godot.Presentation
{
	public partial class GameHUD : CanvasLayer
	{
		[Export] public Node BuilderController;
		[Export] public TeamSide FocusTeam = TeamSide.Left;
		[Export] public string VersionText = "v0.1.0 pre-alpha";
		[Export] public int WoodAmount = 0;

		private Label _hotkeysLabel;
		private Label _goldLabel;
		private Label _woodLabel;
		private Label _timeLabel;
		private Label _versionLabel;
		private Control _inspectorPanel;
		private ColorRect _portraitPlaceholder;
		private Label _portraitRoleBadge;
		private Label _titleLabel;
		private Label _subtitleLabel;
		private ProgressBar _healthBar;
		private Label _healthLabel;
		private Label _damageLabel;
		private Label _rangeLabel;
		private Label _speedLabel;
		private Label _armorLabel;
		private Label _statsLabel;
		private Control _actionPanel;
		private Button _actionButton1;
		private Button _actionButton2;
		private Button _actionButton3;
		private Button _actionButton4;
		private Button _actionButton5;
		private Button _actionButton6;
		private Node3D _selectedNode;
		private bool _isConstructorBuildMenuOpen;
		private int _selectedConstructorTowerIndex = -1;

		private EconomyQueryService _economyQuery;
		private MatchQueryService _matchQuery;
		private RosterState _rosterState;

		public override void _Ready()
		{
			_economyQuery = CompositionRoot.App.Container.Resolve<EconomyQueryService>();
			_matchQuery = CompositionRoot.App.Container.Resolve<MatchQueryService>();
			_rosterState = CompositionRoot.App.Container.Resolve<RosterState>();
			BuilderController ??= GetNodeOrNull<Node>("../BuilderController");

			_hotkeysLabel = GetNode<Label>("TopBar/Margin/TopBarRow/LeftGroup/HotkeysLabel");
			_goldLabel = GetNode<Label>("TopBar/Margin/TopBarRow/CenterGroup/GoldLabel");
			_woodLabel = GetNode<Label>("TopBar/Margin/TopBarRow/CenterGroup/WoodLabel");
			_timeLabel = GetNode<Label>("TopBar/Margin/TopBarRow/CenterGroup/TimeLabel");
			_versionLabel = GetNode<Label>("TopBar/Margin/TopBarRow/VersionLabel");
			_inspectorPanel = GetNode<Control>("BottomBar/Margin/BottomRow/SelectionPanel");
			_portraitPlaceholder = GetNode<ColorRect>("BottomBar/Margin/BottomRow/SelectionPanel/Margin/CenterContainer/SelectionRow/PortraitPanel/Margin/VBox/PortraitPlaceholder");
			_portraitRoleBadge = GetNode<Label>("BottomBar/Margin/BottomRow/SelectionPanel/Margin/CenterContainer/SelectionRow/PortraitPanel/Margin/VBox/PortraitRoleBadge");
			_titleLabel = GetNode<Label>("BottomBar/Margin/BottomRow/SelectionPanel/Margin/CenterContainer/SelectionRow/DetailsPanel/HeaderBox/TitleLabel");
			_subtitleLabel = GetNode<Label>("BottomBar/Margin/BottomRow/SelectionPanel/Margin/CenterContainer/SelectionRow/DetailsPanel/HeaderBox/SubtitleLabel");
			_healthBar = GetNode<ProgressBar>("BottomBar/Margin/BottomRow/SelectionPanel/Margin/CenterContainer/SelectionRow/DetailsPanel/HealthBar");
			_healthLabel = GetNode<Label>("BottomBar/Margin/BottomRow/SelectionPanel/Margin/CenterContainer/SelectionRow/DetailsPanel/HealthLabel");
			_damageLabel = GetNode<Label>("BottomBar/Margin/BottomRow/SelectionPanel/Margin/CenterContainer/SelectionRow/DetailsPanel/StatsGrid/StatDamageLabel");
			_rangeLabel = GetNode<Label>("BottomBar/Margin/BottomRow/SelectionPanel/Margin/CenterContainer/SelectionRow/DetailsPanel/StatsGrid/StatRangeLabel");
			_speedLabel = GetNode<Label>("BottomBar/Margin/BottomRow/SelectionPanel/Margin/CenterContainer/SelectionRow/DetailsPanel/StatsGrid/StatSpeedLabel");
			_armorLabel = GetNode<Label>("BottomBar/Margin/BottomRow/SelectionPanel/Margin/CenterContainer/SelectionRow/DetailsPanel/StatsGrid/StatArmorLabel");
			_statsLabel = GetNode<Label>("BottomBar/Margin/BottomRow/SelectionPanel/Margin/CenterContainer/SelectionRow/DetailsPanel/StatsLabel");
			_actionPanel = GetNode<Control>("BottomBar/Margin/BottomRow/ActionsPanel");
			_actionButton1 = GetNode<Button>("BottomBar/Margin/BottomRow/ActionsPanel/Margin/VBox/ActionPanel/BtnAction1");
			_actionButton2 = GetNode<Button>("BottomBar/Margin/BottomRow/ActionsPanel/Margin/VBox/ActionPanel/BtnAction2");
			_actionButton3 = GetNode<Button>("BottomBar/Margin/BottomRow/ActionsPanel/Margin/VBox/ActionPanel/BtnAction3");
			_actionButton4 = GetNode<Button>("BottomBar/Margin/BottomRow/ActionsPanel/Margin/VBox/ActionPanel/BtnAction4");
			_actionButton5 = GetNode<Button>("BottomBar/Margin/BottomRow/ActionsPanel/Margin/VBox/ActionPanel/BtnAction5");
			_actionButton6 = GetNode<Button>("BottomBar/Margin/BottomRow/ActionsPanel/Margin/VBox/ActionPanel/BtnAction6");

			// UI in bottom-right must stay above world interactions.
			_inspectorPanel.MouseFilter = Control.MouseFilterEnum.Stop;
			_actionPanel.MouseFilter = Control.MouseFilterEnum.Stop;
			_actionButton1.MouseFilter = Control.MouseFilterEnum.Stop;
			_actionButton2.MouseFilter = Control.MouseFilterEnum.Stop;
			_actionButton3.MouseFilter = Control.MouseFilterEnum.Stop;
			_actionButton4.MouseFilter = Control.MouseFilterEnum.Stop;
			_actionButton5.MouseFilter = Control.MouseFilterEnum.Stop;
			_actionButton6.MouseFilter = Control.MouseFilterEnum.Stop;

			_actionButton1.ButtonDown += HandleAction1Pressed;
			_actionButton2.ButtonDown += HandleAction2Pressed;
			_actionButton3.ButtonDown += HandleAction3Pressed;
			_actionButton4.ButtonDown += HandleAction4Pressed;

			_hotkeysLabel.Text = "F9 Menu   F10 Allies   F11 Chat   F12 Help";
			_versionLabel.Text = VersionText;

			HideAllActions();
			UpdateInspectorDisplay();
			UpdateActionPanel();
		}

		public override void _Process(double delta)
		{
			RefreshTopBar();

			if (_selectedNode != null && !GodotObject.IsInstanceValid(_selectedNode))
			{
				ClearSelection();
				return;
			}

			UpdateInspectorDisplay();
			UpdateActionPanel();
		}

		public void SetSelection(Node3D entity)
		{
			bool selectionChanged = _selectedNode != entity;
			_selectedNode = entity;

			if (BuilderController is ConstructionInput builder)
			{
				builder.SetSelectedEntity(entity);
			}
			else
			{
				BuilderController?.Call("SetSelectedEntity", entity);
			}

			if (selectionChanged)
			{
				_isConstructorBuildMenuOpen = false;
				_selectedConstructorTowerIndex = -1;
			}
			UpdateInspectorDisplay();
			UpdateActionPanel();
		}

		public void ClearSelection()
		{
			_selectedNode = null;

			if (BuilderController is ConstructionInput builder)
			{
				builder.ClearSelectedEntity();
			}
			else
			{
				BuilderController?.Call("ClearSelectedEntity");
			}

			_isConstructorBuildMenuOpen = false;
			_selectedConstructorTowerIndex = -1;
			UpdateInspectorDisplay();
			UpdateActionPanel();
		}

		private void UpdateInspectorDisplay()
		{
			var viewData = SelectionViewFactory.Build(_selectedNode);
			_titleLabel.Text = viewData.Title;
			_subtitleLabel.Text = viewData.Subtitle;
			_portraitRoleBadge.Text = viewData.RoleBadge;
			_portraitPlaceholder.Color = viewData.PortraitColor;
			SetHealth(viewData.CurrentHealth, viewData.MaxHealth);
			SetStats(
				viewData.DamageText,
				viewData.RangeText,
				viewData.SpeedText,
				viewData.ArmorText);
			_statsLabel.Text = viewData.DescriptionText;
		}

		private void UpdateActionPanel()
		{
			HideAllActions();
			ClearBuildButtonHighlights();
			var slots = ActionPanelViewFactory.Build(
				_selectedNode,
				_isConstructorBuildMenuOpen,
				_selectedConstructorTowerIndex,
				_economyQuery,
				_rosterState);

			ApplyActionSlot(_actionButton1, slots[0]);
			ApplyActionSlot(_actionButton2, slots[1]);
			ApplyActionSlot(_actionButton3, slots[2]);
			ApplyActionSlot(_actionButton4, slots[3]);
			ApplyActionSlot(_actionButton5, slots[4]);
			ApplyActionSlot(_actionButton6, slots[5]);
		}

		private void HideAllActions()
		{
			ConfigureEmptyActionSlot(_actionButton1);
			ConfigureEmptyActionSlot(_actionButton2);
			ConfigureEmptyActionSlot(_actionButton3);
			ConfigureEmptyActionSlot(_actionButton4);
			ConfigureEmptyActionSlot(_actionButton5);
			ConfigureEmptyActionSlot(_actionButton6);

			_actionButton1.Visible = false;
			_actionButton2.Visible = false;
			_actionButton3.Visible = false;
			_actionButton4.Visible = false;
			_actionButton5.Visible = true;
			_actionButton6.Visible = true;
		}

		private void HandleAction1Pressed()
		{
			if (_selectedNode is TowerAdapter tower)
			{
				if (BuilderController is ConstructionInput builder)
				{
					builder.TrySellTower(tower);
				}
				else
				{
					BuilderController?.Call("TrySellTower", tower);
				}
				ClearSelection();
				return;
			}

			if (_selectedNode is ConstructorAdapter)
			{
				if (_isConstructorBuildMenuOpen)
				{
					if (BuilderController is ConstructionInput builder)
					{
						builder.BeginBuildPlacementFromUi(_selectedNode, 0);
					}
					else
					{
						BuilderController?.Call("SetSelectedEntity", _selectedNode);
						BuilderController?.Call("BeginBuildPlacementFromSelection", 0);
					}
					_selectedConstructorTowerIndex = 0;
					UpdateActionPanel();
				}
				else
				{
					_isConstructorBuildMenuOpen = true;
					_selectedConstructorTowerIndex = -1;
					if (BuilderController is ConstructionInput builder)
					{
						builder.EnterBuildMenuForSelectedConstructor();
					}
					UpdateActionPanel();
				}
			}
		}

		private void HandleAction2Pressed()
		{
			if (_selectedNode is TowerAdapter tower)
			{
				if (BuilderController is ConstructionInput builder)
				{
					builder.TryUpgradeTower(tower);
				}
				else
				{
					BuilderController?.Call("TryUpgradeTower", tower);
				}
				UpdateActionPanel();
				UpdateInspectorDisplay();
				return;
			}

			if (_selectedNode is ConstructorAdapter)
			{
				if (_isConstructorBuildMenuOpen)
				{
					if (BuilderController is ConstructionInput builder)
					{
						builder.BeginBuildPlacementFromUi(_selectedNode, 1);
					}
					else
					{
						BuilderController?.Call("SetSelectedEntity", _selectedNode);
						BuilderController?.Call("BeginBuildPlacementFromSelection", 1);
					}
					_selectedConstructorTowerIndex = 1;
					UpdateActionPanel();
				}
			}
		}

		private void HandleAction3Pressed()
		{
			if (_selectedNode is ConstructorAdapter && _isConstructorBuildMenuOpen)
			{
				_isConstructorBuildMenuOpen = false;
				_selectedConstructorTowerIndex = -1;
				if (BuilderController is ConstructionInput builder)
				{
					builder.ExitBuildPlacementMode();
				}
				else
				{
					BuilderController?.Call("ExitBuildPlacementMode");
				}
				UpdateActionPanel();
			}
		}

		private void HandleAction4Pressed() { }

		public bool IsPointOverUi(Vector2 screenPoint)
		{
			var topBar = GetNodeOrNull<Control>("TopBar");
			if (topBar != null && topBar.Visible)
			{
				var topBarRect = new Rect2(topBar.GlobalPosition, topBar.Size);
				if (topBarRect.HasPoint(screenPoint)) return true;
			}

			var bottomBar = GetNodeOrNull<Control>("BottomBar");
			if (bottomBar != null && bottomBar.Visible)
			{
				var bottomBarRect = new Rect2(bottomBar.GlobalPosition, bottomBar.Size);
				if (bottomBarRect.HasPoint(screenPoint)) return true;
			}

			if (_actionPanel != null && _actionPanel.Visible)
			{
				var actionRect = new Rect2(_actionPanel.GlobalPosition, _actionPanel.Size);
				if (actionRect.HasPoint(screenPoint)) return true;
			}

			if (_inspectorPanel != null && _inspectorPanel.Visible)
			{
				var inspectorRect = new Rect2(_inspectorPanel.GlobalPosition, _inspectorPanel.Size);
				if (inspectorRect.HasPoint(screenPoint)) return true;
			}

			return false;
		}

		private void ApplySelectedStyle(Button button, Color borderColor)
		{
			var style = new StyleBoxFlat
			{
				BgColor = new Color(0.13f, 0.13f, 0.13f, 0.95f),
				BorderColor = borderColor,
				BorderWidthLeft = 3,
				BorderWidthTop = 3,
				BorderWidthRight = 3,
				BorderWidthBottom = 3,
				CornerRadiusTopLeft = 6,
				CornerRadiusTopRight = 6,
				CornerRadiusBottomRight = 6,
				CornerRadiusBottomLeft = 6
			};

			button.AddThemeStyleboxOverride("normal", style);
			button.AddThemeStyleboxOverride("hover", style);
			button.AddThemeStyleboxOverride("pressed", style);
			button.AddThemeStyleboxOverride("focus", style);
		}

		private void ClearBuildButtonHighlights()
		{
			_actionButton1.RemoveThemeStyleboxOverride("normal");
			_actionButton1.RemoveThemeStyleboxOverride("hover");
			_actionButton1.RemoveThemeStyleboxOverride("pressed");
			_actionButton1.RemoveThemeStyleboxOverride("focus");

			_actionButton2.RemoveThemeStyleboxOverride("normal");
			_actionButton2.RemoveThemeStyleboxOverride("hover");
			_actionButton2.RemoveThemeStyleboxOverride("pressed");
			_actionButton2.RemoveThemeStyleboxOverride("focus");
		}

		private void RefreshTopBar()
		{
			_goldLabel.Text = $"Gold: {_economyQuery.GetGold(FocusTeam)}";
			_woodLabel.Text = $"Wood: {WoodAmount}";

			if (_matchQuery.IsBuildPhase())
			{
				double remaining = MatchAdapter.Instance?.BuildPhaseTimeRemaining ?? 0.0;
				_timeLabel.Text = $"Build: {Mathf.CeilToInt((float)remaining)}s";
				return;
			}

			string phaseLabel = _matchQuery.GetCurrentPhase() switch
			{
				MatchPhase.CombatPhase => "Combat",
				MatchPhase.WaitingForPlayers => "Waiting",
				_ => "Match"
			};

			_timeLabel.Text = phaseLabel;
		}

		private void SetHealth(float current, float max)
		{
			float safeMax = Mathf.Max(max, 1f);
			bool hasHealth = max > 0f;
			_healthBar.MaxValue = safeMax;
			_healthBar.Value = hasHealth ? Mathf.Clamp(current, 0f, safeMax) : 0f;
			_healthLabel.Text = hasHealth ? $"HP: {current:0.#} / {max:0.#}" : "HP: N/A";
		}

		private void SetStats(string damage, string range, string speed, string armor)
		{
			_damageLabel.Text = damage;
			_rangeLabel.Text = range;
			_speedLabel.Text = speed;
			_armorLabel.Text = armor;
		}

		private static void ConfigureEmptyActionSlot(Button button)
		{
			button.Text = "";
			button.Disabled = true;
		}

		private void ApplyActionSlot(Button button, ActionSlotViewData slot)
		{
			button.Visible = slot.Visible;
			button.Text = slot.Text;
			button.Disabled = slot.Disabled;

			if (slot.HighlightColor is Color color)
			{
				ApplySelectedStyle(button, color);
			}
		}
	}
}
