using Godot;

namespace LegionTDClone.Platform.Godot.Simulation
{
    public partial class ProjectileAdapter : Node3D
    {
        private UnitAdapter _target;
        private Vector3 _lastKnownTargetPos;
        private float _damage;
        private float _speed;
        private bool _isEnemyProjectile;

        public void Initialize(UnitAdapter target, float damage, float speed, bool isEnemyProjectile)
        {
            _target = target;
            _damage = damage;
            _speed = speed;
            _isEnemyProjectile = isEnemyProjectile;
        }

        public override void _Ready()
        {
            var meshNode = new MeshInstance3D();
            var sphere = new SphereMesh { Radius = 0.15f, Height = 0.3f };
            var material = new StandardMaterial3D
            {
                EmissionEnabled = true,
                Emission = _isEnemyProjectile ? new Color(1f, 0.35f, 0.35f, 1f) : new Color(0.35f, 1f, 0.45f, 1f),
                AlbedoColor = _isEnemyProjectile ? new Color(1f, 0.35f, 0.35f, 1f) : new Color(0.35f, 1f, 0.45f, 1f)
            };
            sphere.Material = material;
            meshNode.Mesh = sphere;
            AddChild(meshNode);
        }

        public override void _Process(double delta)
        {
            if (!Multiplayer.IsServer())
            {
                QueueFree();
                return;
            }

            Vector3 targetPos = _lastKnownTargetPos;
            if (_target != null && GodotObject.IsInstanceValid(_target))
            {
                targetPos = _target.GlobalPosition + new Vector3(0, 1.0f, 0);
                _lastKnownTargetPos = targetPos;
            }

            Vector3 direction = GlobalPosition.DirectionTo(targetPos);
            GlobalPosition += direction * _speed * (float)delta;

            if (GlobalPosition.DistanceTo(targetPos) <= 0.35f)
            {
                if (_target != null && GodotObject.IsInstanceValid(_target))
                {
                    _target.ReceiveDirectDamage(_damage);
                }
                QueueFree();
            }
        }
    }
}
