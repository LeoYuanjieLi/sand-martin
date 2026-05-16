using Grasshopper.Kernel;

namespace SandMartin.Host.Services
{
    public abstract class GrasshopperServiceBase
    {
        protected bool IsRunningInRhino()
        {
            try {
                return Grasshopper.Instances.ActiveCanvas?.Document != null;
            } catch {
                return false;
            }
        }
    }
}
