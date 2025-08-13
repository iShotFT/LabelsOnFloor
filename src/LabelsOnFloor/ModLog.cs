using Verse;

namespace LabelsOnFloor
{
    public static class ModLog
    {
        private const string Prefix = "[LabelsOnFloor] ";
        
        public static void Message(string message)
        {
            Log.Message(Prefix + message);
        }
        
        public static void Warning(string message)
        {
            Log.Warning(Prefix + message);
        }
        
        public static void Error(string message)
        {
            Log.Error(Prefix + message);
        }
        
        public static void ErrorOnce(string message, int key)
        {
            Log.ErrorOnce(Prefix + message, key);
        }
    }
}