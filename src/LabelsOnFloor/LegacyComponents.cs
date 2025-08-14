using Verse;

namespace LabelsOnFloor
{
    /// <summary>
    /// Legacy GameComponent for backwards compatibility with old saves.
    /// This component is no longer used but needs to exist so old saves can load without errors.
    /// </summary>
    public class GameComponent_LabelsOnFloor : GameComponent
    {
        public GameComponent_LabelsOnFloor()
        {
        }
        
        public GameComponent_LabelsOnFloor(Game game) : base()
        {
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            // No data to save/load - this is just a stub for compatibility
        }
    }
    
    /// <summary>
    /// Legacy MapComponent for backwards compatibility with old saves.
    /// This component is no longer used but needs to exist so old saves can load without errors.
    /// </summary>
    public class MapComponent_LabelsOnFloor : MapComponent
    {
        public MapComponent_LabelsOnFloor(Map map) : base(map)
        {
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            // No data to save/load - this is just a stub for compatibility
        }
    }
}