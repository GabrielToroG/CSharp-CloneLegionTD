using Godot;
using LegionTDClone.Domain.Combat;
using System;

namespace LegionTDClone.Platform.Godot.Simulation
{
    public partial class UnitAdapter : CharacterBody3D
    {
        public static event Action<UnitAdapter> OnEnemyAggroAcquired;

        // Initial setup data (to be passed to CombatEntity in ready)
        [Export] public float MaxHealth = 100f;
        [Export] public float AttackDamage = 10f;
        [Export] public float AttackRange = 2f;
        [Export] public float AggroRange = 8f;
        [Export] public float AttackSpeed = 1f;
        [Export] public float MovementSpeed = 3f;
        [Export] public float Armor = 0f;
        [Export] public bool IsEnemy = false;
        [Export] public bool UseProjectileAttack = false;
        [Export] public float ProjectileSpeed = 18f;
        [Export] public float HealthBarYOffset = 2.4f;
        [Export] public float HealthBarWidth = 1.7f;
        [Export] public float HealthBarHeight = 0.14f;

        public CombatEntity EntityState { get; private set; }
        private CombatService _combatService;
        private Node3D _healthBarRoot;
        private MeshInstance3D _healthBarFill;
        private MeshInstance3D _healthBarBg;
        private QuadMesh _healthBarFillMesh;
        private float _resolvedHealthBarWidth;
        private float _resolvedHealthBarHeight;

        private double _attackCooldown = 0;
        private double _targetRefreshTimer = 0;
        
        public UnitAdapter CurrentTarget { get; private set; }
        
        public Vector3 FinalDestination { get; set; }
        public bool HasTargetDestination { get; set; } = false;
        private bool _hasReportedAggro;

        public override void _Ready()
        {
            // Initialize Domain Entity
            EntityState = new CombatEntity(
                System.Guid.NewGuid().ToString(),
                IsEnemy,
                MaxHealth,
                AttackDamage,
                AttackRange,
                AttackSpeed,
                Armor,
                MovementSpeed
            );
            
            _combatService = new CombatService();

            EntityState.OnDeath += HandleDeath;
            
            AddToGroup("Units");
            BuildHealthBar();
            ApplyShadowPolicy();
            UpdateHealthBar();
        }

        private void HandleDeath()
        {
            QueueFree();
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!Multiplayer.IsServer()) return;

            if (_attackCooldown > 0)
            {
                _attackCooldown -= delta;
            }

            if (CurrentTarget == null || CurrentTarget.EntityState.IsDead || !GodotObject.IsInstanceValid(CurrentTarget))
            {
                CurrentTarget = null;
                FindTarget();
                _targetRefreshTimer = 0.5;
            }
            else
            {
                _targetRefreshTimer -= delta;
                if (_targetRefreshTimer <= 0)
                {
                    FindTarget();
                    _targetRefreshTimer = 0.5;
                }
            }

            if (CurrentTarget != null)
            {
                float distance = GlobalPosition.DistanceTo(CurrentTarget.GlobalPosition);
                if (distance <= EntityState.AttackRange)
                {
                    if (_attackCooldown <= 0)
                    {
                        _attackCooldown = 1.0f / EntityState.AttackSpeed;
                        if (UseProjectileAttack)
                        {
                            SpawnProjectile(CurrentTarget, EntityState.AttackDamage);
                        }
                        else
                        {
                            // Execute Attack via Domain
                            _combatService.ExecuteAttack(EntityState, CurrentTarget.EntityState);
                            // Network synchronization
                            CurrentTarget.Rpc(MethodName.RpcReceiveDamage, EntityState.AttackDamage);
                        }
                    }
                }
                else
                {
                    MoveTowardsTarget(CurrentTarget.GlobalPosition);
                }
            }
            else if (HasTargetDestination)
            {
                float distance = GlobalPosition.DistanceTo(FinalDestination);
                if (distance <= 1.0f)
                {
                    // Die on reaching end (leak logic future)
                    EntityState.TakeDamage(9999f); 
                }
                else
                {
                    MoveTowardsTarget(FinalDestination);
                }
            }
        }

        private void MoveTowardsTarget(Vector3 targetPos)
        {
            Vector3 direction = GlobalPosition.DirectionTo(targetPos);
            
            // Boids Separation
            Vector3 separation = Vector3.Zero;
            var allUnits = GetTree().GetNodesInGroup("Units");
            foreach (var node in allUnits)
            {
                if (node is UnitAdapter otherUnit && otherUnit != this && otherUnit.IsEnemy == this.IsEnemy && !otherUnit.EntityState.IsDead)
                {
                    float distSq = GlobalPosition.DistanceSquaredTo(otherUnit.GlobalPosition);
                    if (distSq < 2.0f && distSq > 0.001f)
                    {
                        Vector3 pushDir = otherUnit.GlobalPosition.DirectionTo(GlobalPosition);
                        separation += pushDir * (2.0f - Mathf.Sqrt(distSq));
                    }
                }
            }

            if (separation != Vector3.Zero)
            {
                direction = (direction + separation * 1.5f).Normalized();
            }

            Velocity = direction * EntityState.MovementSpeed;
            MoveAndSlide();
            
            if (direction.LengthSquared() > 0.001f)
            {
                Vector3 lookTarget = GlobalPosition + direction;
                try { LookAt(lookTarget, Vector3.Up); } catch { }
            }
        }

        private void FindTarget()
        {
            var allUnits = GetTree().GetNodesInGroup("Units");
            float closestDistance = float.MaxValue;
            UnitAdapter closestUnit = null;

            foreach (var node in allUnits)
            {
                if (node is UnitAdapter otherUnit && otherUnit != this && otherUnit.IsEnemy != this.IsEnemy && !otherUnit.EntityState.IsDead)
                {
                    float dist = GlobalPosition.DistanceTo(otherUnit.GlobalPosition);
                    if (dist <= AggroRange && dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestUnit = otherUnit;
                    }
                }
            }
            CurrentTarget = closestUnit;

            // Trigger once per unit when an enemy wave unit first acquires aggro.
            if (IsEnemy && CurrentTarget != null && !_hasReportedAggro)
            {
                _hasReportedAggro = true;
                OnEnemyAggroAcquired?.Invoke(this);
            }
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
        public void RpcReceiveDamage(float damage)
        {
            // Sync visual hp or let domain handle if host
            if (!Multiplayer.IsServer())
            {
               if (EntityState != null && !EntityState.IsDead)
               {
                   EntityState.TakeDamage(damage);
               }
            }
            UpdateHealthBar();
        }

        public void ReceiveDirectDamage(float damage)
        {
            if (EntityState == null || EntityState.IsDead) return;
            EntityState.TakeDamage(damage);
            Rpc(MethodName.RpcReceiveDamage, damage);
            UpdateHealthBar();
        }

        private void SpawnProjectile(UnitAdapter target, float damage)
        {
            if (target == null || !GodotObject.IsInstanceValid(target)) return;

            var projectile = new ProjectileAdapter();
            GetTree().Root.AddChild(projectile);
            projectile.GlobalPosition = GlobalPosition + new Vector3(0, 1.0f, 0);
            projectile.Initialize(target, damage, ProjectileSpeed, IsEnemy);
        }

        public override void _Process(double delta)
        {
            UpdateHealthBar();
        }

        private void BuildHealthBar()
        {
            ResolveHealthBarDimensions(out float yOffset, out float width, out float height);
            _resolvedHealthBarWidth = width;
            _resolvedHealthBarHeight = height;

            _healthBarRoot = new Node3D
            {
                Name = "HealthBarRoot",
                Position = new Vector3(0, yOffset, 0)
            };
            AddChild(_healthBarRoot);

            _healthBarBg = new MeshInstance3D();
            var bgMesh = new QuadMesh { Size = new Vector2(width, height) };
            var bgMat = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
                AlbedoColor = new Color(0f, 0f, 0f, 0.95f),
                NoDepthTest = true
            };
            bgMat.RenderPriority = -1;
            bgMesh.Material = bgMat;
            _healthBarBg.Mesh = bgMesh;
            _healthBarBg.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            _healthBarRoot.AddChild(_healthBarBg);

            _healthBarFill = new MeshInstance3D();
            _healthBarFillMesh = new QuadMesh { Size = new Vector2(width, height * 0.75f) };
            var fillMat = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
                AlbedoColor = new Color(0.2f, 0.95f, 0.3f, 1f),
                NoDepthTest = true
            };
            fillMat.RenderPriority = 1;
            _healthBarFillMesh.Material = fillMat;
            _healthBarFill.Mesh = _healthBarFillMesh;
            _healthBarFill.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
            _healthBarFill.Position = new Vector3(0, 0, -0.01f);
            _healthBarRoot.AddChild(_healthBarFill);
        }

        private void ResolveHealthBarDimensions(out float yOffset, out float width, out float height)
        {
            float maxWidth = 0f;
            float maxTop = 0f;

            foreach (Node child in GetChildren())
            {
                if (child is not MeshInstance3D mesh || mesh.Mesh == null) continue;

                Aabb localBounds = mesh.Mesh.GetAabb();
                Vector3 scaledSize = localBounds.Size * mesh.Scale.Abs();
                Vector3 scaledPosition = localBounds.Position * mesh.Scale.Abs() + mesh.Position;
                float meshTop = scaledPosition.Y + scaledSize.Y;

                maxWidth = Mathf.Max(maxWidth, Mathf.Max(scaledSize.X, scaledSize.Z));
                maxTop = Mathf.Max(maxTop, meshTop);
            }

            width = Mathf.Max(HealthBarWidth, Mathf.Max(maxWidth * 1.15f, 2.4f));
            height = Mathf.Max(HealthBarHeight, Mathf.Max(width * 0.16f, 0.22f));
            yOffset = Mathf.Max(HealthBarYOffset, maxTop + (height * 1.6f));
        }

        private void ApplyShadowPolicy()
        {
            // Keep combat unit meshes with shadows, but force health bars shadowless.
            foreach (Node child in GetChildren())
            {
                if (child is not MeshInstance3D mesh) continue;

                if (mesh == _healthBarBg || mesh == _healthBarFill)
                {
                    mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
                }
                else
                {
                    mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.On;
                }
            }
        }

        private void UpdateHealthBar()
        {
            if (_healthBarRoot == null || _healthBarFill == null || EntityState == null) return;
            if (!GodotObject.IsInstanceValid(_healthBarRoot) || !GodotObject.IsInstanceValid(_healthBarFill)) return;

            float pct = Mathf.Clamp(EntityState.CurrentHealth / Mathf.Max(1f, EntityState.MaxHealth), 0f, 1f);
            float fillWidth = _resolvedHealthBarWidth * pct;
            _healthBarFillMesh.Size = new Vector2(fillWidth, _resolvedHealthBarHeight * 0.75f);
            // Anchor fill to the right edge so health depletes right-to-left on screen.
            _healthBarFill.Position = new Vector3((_resolvedHealthBarWidth * 0.5f) - (fillWidth * 0.5f), 0, -0.01f);
            _healthBarRoot.Visible = !EntityState.IsDead;
        }
    }
}
