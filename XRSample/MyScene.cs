using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;

namespace XRSample
{
    public class MyScene : Scene
    {
        public override void RegisterManagers()
        {
            base.RegisterManagers();
            this.Managers.AddManager(new Evergine.Bullet.BulletPhysicManager3D());
        }

        protected override void Start()
        {
            base.Start();
            var drawContext = (this.Managers.RenderManager.ActiveCamera3D.DrawContext as ForwardDrawContext);
            drawContext.ZPrePassIsEnabled = false;
            drawContext.GBufferPassIsEnabled = false;
        }
    }
}