using Godot;

namespace LegionTDClone.CompositionRoot
{
    public partial class Bootstrapper : Node
    {
        public static Container GlobalContainer { get; private set; }

        public override void _EnterTree()
        {
            if (GlobalContainer == null)
            {
                GlobalContainer = new Container();
                RegisterServices();
            }
        }

        private void RegisterServices()
        {
            // Registration mappings could go here, e.g.:
            // GlobalContainer.RegisterSingleton(new Domain.Economy.EconomyState());
            // GlobalContainer.RegisterSingleton(new Domain.Match.MatchState());
        }
    }
}
