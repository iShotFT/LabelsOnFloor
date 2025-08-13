using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LabelsOnFloor
{
    public class GameComponent_LabelsOnFloor : GameComponent
    {
        public GameComponent_LabelsOnFloor(Game game) : base()
        {
        }
        
        public override void GameComponentOnGUI()
        {
            // Replaces OnGUI() from HugsLib
            // Check if we're in world view and need to reset labels
            if (WorldRendererUtility.CurrentWorldRenderMode != WorldRenderMode.None)
            {
                LabelsOnFloorMod.Instance?.LabelPlacementHandler?.SetDirty();
            }
        }
        
        public override void LoadedGame()
        {
            // Replaces WorldLoaded() for loaded games
            InitializeWorldComponents();
        }
        
        public override void StartedNewGame()
        {
            // Replaces WorldLoaded() for new games
            InitializeWorldComponents();
        }
        
        private void InitializeWorldComponents()
        {
            if (Find.World == null)
            {
                ModLog.Warning("World is null when trying to initialize world components");
                return;
            }
            
            var customRoomLabelManager = Find.World.GetComponent<CustomRoomLabelManagerComponent>();
            if (customRoomLabelManager == null)
            {
                ModLog.Warning("CustomRoomLabelManagerComponent not found on world");
                return;
            }
            
            LabelsOnFloorMod.Instance?.InitializeWorldComponents(customRoomLabelManager);
            ModLog.Message("World components initialized successfully");
        }
    }
}