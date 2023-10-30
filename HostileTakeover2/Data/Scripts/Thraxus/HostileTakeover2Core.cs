using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Enums;
using HostileTakeover2.Thraxus.Controllers.Loggers;
using HostileTakeover2.Thraxus.Utility;
using HostileTakeover2.Thraxus.Utility.UserConfig.Controllers;
using VRage.Game.Components;

namespace HostileTakeover2.Thraxus
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, priority: int.MinValue + 1)]
    internal class HostileTakeover2Core : BaseSessionComp
    {
        protected override string CompName => nameof(HostileTakeover2Core);
        protected override CompType Type => CompType.Server;
        protected override MyUpdateOrder Schedule => MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation;

        private Mediator _mediator;
        
        protected override void SuperEarlySetup()
        {
            base.SuperEarlySetup();
            
            _mediator = new Mediator(new SettingsController(ModContext.ModName));
            _mediator.OnWriteToLog += WriteGeneral;
        }


        protected override void UpdateBeforeSim()
        {
            base.UpdateBeforeSim();
            _mediator.ActionQueue.Execute();
        }

        protected override void Unload()
        {
            WriteGeneral(nameof(Unload), $"Unloading {ModContext.ModName}");
            _mediator.Close();
            _mediator.OnWriteToLog -= WriteGeneral;
            base.Unload();
        }
    }
}