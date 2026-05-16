using System.Threading.Tasks;
using SandMartin.Host.Models;

namespace SandMartin.Host.Services
{
    /// <summary>
    /// Facade class that orchestrates Grasshopper canvas operations by delegating 
    /// to specialized manager classes (NodeManager, ConnectionManager, StateManager).
    /// </summary>
    public class CanvasManager
    {
        private readonly NodeManager _nodeManager;
        private readonly ConnectionManager _connectionManager;
        private readonly StateManager _stateManager;

        public CanvasManager()
        {
            _nodeManager = new NodeManager();
            _connectionManager = new ConnectionManager();
            _stateManager = new StateManager();
        }

        public virtual Task<string> GetCanvasState() => _stateManager.GetCanvasState();
        
        public virtual Task<string> GetNodeDetails(string nodeId) => _nodeManager.GetNodeDetails(nodeId);
        
        public virtual Task<string> CreateNode(CreateNodeRequest request) => _nodeManager.CreateNode(request);
        
        public virtual Task<string> UpdateNode(UpdateNodeRequest request) => _nodeManager.UpdateNode(request);
        
        public virtual Task<string> DeleteNode(string nodeId) => _nodeManager.DeleteNode(nodeId);
        
        public virtual Task<string> CreateConnection(ConnectionRequest request) => _connectionManager.CreateConnection(request);
        
        public virtual Task<string> DisconnectNode(DisconnectRequest request) => _connectionManager.DisconnectNode(request);
    }
}
